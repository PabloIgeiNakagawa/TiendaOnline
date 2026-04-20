using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
public class PedidoCommandService : IPedidoCommandService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;
        private readonly ILogger<PedidoCommandService> _logger;

        public PedidoCommandService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService, ILogger<PedidoCommandService> logger)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
            _logger = logger;
        }

        public async Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("El pedido no tiene ítems.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // CONSOLIDAMOS items por ProductoId para evitar duplicados
                var itemsAgrupados = dto.Items
                    .GroupBy(i => i.ProductoId)
                    .Select(g => new CrearPedidoDetalleDto
                    {
                        ProductoId = g.Key,
                        Cantidad = g.Sum(x => x.Cantidad),
                        PrecioUnitario = g.First().PrecioUnitario
                    })
                    .ToList();

                var pedido = new Pedido
                {
                    UsuarioId = dto.UsuarioId,
                    MetodoDePagoId = dto.MetodoDePagoId,
                    EsEnvioADomicilio = dto.EsEnvioADomicilio,
                    EnvioCalle = dto.EnvioCalle,
                    EnvioNumero = dto.EnvioNumero,
                    EnvioPiso = dto.EnvioPiso,
                    EnvioDepartamento = dto.EnvioDepartamento,
                    EnvioObservaciones = dto.EnvioObservaciones,
                    EnvioLocalidad = dto.EnvioLocalidad,
                    EnvioProvincia = dto.EnvioProvincia,
                    EnvioCodigoPostal = dto.EnvioCodigoPostal,
                    CostoEnvio = dto.CostoEnvio,
                    FechaPedido = DateTime.Now,
                    Estado = EstadoPedido.Nuevo,
                    EstadoPago = EstadoPago.Pendiente,
                    DetallesPedido = new List<DetallePedido>()
                };

                foreach (var itemDto in itemsAgrupados)
                {
                    // Guardamos el snapshot del pedido, pero el stock se reservará recién cuando el pago sea aprobado.
                    var producto = await _context.Productos.FindAsync(itemDto.ProductoId);
                    if (producto == null)
                        throw new Exception($"No se encontró el producto con ID {itemDto.ProductoId}.");

                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = itemDto.Cantidad,
                        PrecioUnitario = producto.Precio,
                        Producto = producto // Importante para que el PaymentService tenga el nombre
                    });
                }

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // RECARGAMOS EL PEDIDO CON SUS INCLUDES
                var pedidoCompleto = await _context.Pedidos
                    .Include(p => p.Usuario) // Traemos los datos del usuario (Email)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto) // Traemos los datos del producto (Nombre)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedido.PedidoId);

                if (pedidoCompleto == null) throw new Exception("Error al recuperar el pedido creado.");

                // Enviar email de confirmación usando Hangfire (Persistente y con reintentos)
                var subtotal = pedidoCompleto.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
                var total = subtotal + pedidoCompleto.CostoEnvio;

                BackgroundJob.Enqueue<IEmailService>(x => x.EnviarConfirmacionPedidoAsync(
                    pedidoCompleto.Usuario.Email,
                    pedidoCompleto.Usuario.Nombre,
                    pedidoCompleto.PedidoId,
                    total
                ));

                return new PedidoPagoDto
                {
                    PedidoId = pedidoCompleto.PedidoId,
                    EmailUsuario = pedidoCompleto.Usuario.Email,
                    Items = pedidoCompleto.DetallesPedido.Select(d => new ItemPagoDto
                    {
                        Nombre = d.Producto.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido.");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ConfirmarPagoAsync(InfoPagoDto infoPago)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.PedidoId == infoPago.PedidoId);

                // Idempotencia y existencia: Si no existe o ya está pagado, cortamos acá
                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el pedido {PedidoId} para confirmar pago.", infoPago.PedidoId);
                    return false;
                }

                // Calculamos el total real (Subtotal + Envío) con precisión decimal
                decimal subtotal = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
                decimal totalCalculado = subtotal + pedido.CostoEnvio;
                var estadoNormalizado = infoPago.Estado?.Trim().ToLowerInvariant();
                var pedidoVencido = pedido.EstaVencido();

                // Seguridad: solo validamos monto cuando el pago viene efectivamente aprobado.
                if (estadoNormalizado == "approved" && Math.Abs(infoPago.MontoPagado - totalCalculado) > 0.01m)
                {
                    _logger.LogWarning("Mismatch de montos en pedido {PedidoId}. MP: {MontoMP}, Sistema: {MontoSistema} (Subtotal: {Subtotal}, Envío: {Envio}), TransaccionId: {TransaccionId}",
                        infoPago.PedidoId, infoPago.MontoPagado, totalCalculado, subtotal, pedido.CostoEnvio, infoPago.TransaccionId);
                    return false;
                }

                bool resultadoEfectivo = false;
                string? tipoEmailAEnviar = null;
                var nombreCompletoUsuario = string.Join(" ", new[] { pedido.Usuario.Nombre, pedido.Usuario.Apellido }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                switch (estadoNormalizado)
                {
                    case "approved":
                        if (pedido.EstadoPago == EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba aprobado. Ignorando.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedidoVencido)
                        {
                            _logger.LogWarning("El pedido {PedidoId} ya venció. Ignorando aprobación tardía. Transacción: {TransaccionId}",
                                infoPago.PedidoId, infoPago.TransaccionId);
                            return false;
                        }

                        if (pedido.Estado == EstadoPedido.Cancelado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba cancelado. Ignorando aprobación tardía.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando aprobación tardía.", infoPago.PedidoId);
                            return false;
                        }

                        var reservaStockExitosa = await IntentarReservarStockAsync(pedido);
                        if (!reservaStockExitosa)
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }

                        foreach (var detalle in pedido.DetallesPedido.Where(d => d.Producto != null))
                        {
                            _movimientoStockCommandService.GenerarMovimiento(
                                detalle.Producto!,
                                -detalle.Cantidad,
                                TipoMovimiento.SalidaVenta,
                                pedido.PedidoId,
                                $"Venta confirmada por pago aprobado en Pedido #{pedido.PedidoId}"
                            );
                        }

                        pedido.EstadoPago = EstadoPago.Aprobado;
                        pedido.Estado = EstadoPedido.EnPreparacion;
                        pedido.FechaEnPreparacion = DateTime.Now;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        tipoEmailAEnviar = "aprobado";
                        break;

                    case "rejected":
                    case "declined":
                        if (pedido.Estado == EstadoPedido.Cancelado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba cancelado. Ignorando rechazo posterior.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago == EstadoPago.Rechazado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba rechazado. Ignorando.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando rechazo posterior.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago == EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba aprobado. Ignorando rechazo posterior.", infoPago.PedidoId);
                            return false;
                        }

                        pedido.EstadoPago = EstadoPago.Rechazado;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        tipoEmailAEnviar = "rechazado";
                        break;

                    case "cancelled":
                        if (pedido.Estado == EstadoPedido.Cancelado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba cancelado. Ignorando.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando cancelación posterior.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago == EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba aprobado. Ignorando cancelación posterior.", infoPago.PedidoId);
                            return false;
                        }

                        pedido.EstadoPago = EstadoPago.Rechazado;
                        pedido.Estado = EstadoPedido.Cancelado;
                        pedido.FechaCancelado = DateTime.Now;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        tipoEmailAEnviar = "rechazado";
                        break;

                    case "refunded":
                    case "charged_back":
                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando.", infoPago.PedidoId);
                            return false;
                        }

                        if (pedido.EstadoPago != EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} no estaba aprobado. Ignorando reembolso para estado {EstadoPago}.", infoPago.PedidoId, pedido.EstadoPago);
                            return false;
                        }

                        pedido.EstadoPago = EstadoPago.Reembolsado;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        tipoEmailAEnviar = "reembolsado";
                        break;
                }

if (resultadoEfectivo)
                {
                    switch (tipoEmailAEnviar)
                    {
                        case "aprobado":
                            BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoExitosoAsync(
                                pedido.Usuario.Email,
                                nombreCompletoUsuario,
                                pedido.PedidoId,
                                totalCalculado));
                            break;

                        case "rechazado":
                            BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoRechazadoAsync(
                                pedido.Usuario.Email,
                                nombreCompletoUsuario,
                                pedido.PedidoId,
                                totalCalculado));
                            break;

                        case "reembolsado":
                            BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoReembolsadoAsync(
                                pedido.Usuario.Email,
                                nombreCompletoUsuario,
                                pedido.PedidoId,
                                totalCalculado));
                            break;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Pedido {PedidoId} procesado con estado MP: {Estado}. Transacción: {TransaccionId}",
                        infoPago.PedidoId, estadoNormalizado, infoPago.TransaccionId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al procesar el pago del pedido {PedidoId}", infoPago.PedidoId);
                throw;
            }
        }

        public async Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

            if (pedido == null) return null;

            // Solo permitimos reintentar si el pedido sigue pendiente
            if (pedido.EstadoPago == EstadoPago.Aprobado)
                throw new Exception("Este pedido ya ha sido pagado.");

            if (pedido.Estado == EstadoPedido.Cancelado || pedido.EstaVencido())
                throw new Exception("Este pedido ya no admite nuevos intentos de pago.");

            return new PedidoPagoDto
            {
                PedidoId = pedido.PedidoId,
                EmailUsuario = pedido.Usuario.Email,
                Items = pedido.DetallesPedido.Select(d => new ItemPagoDto
                {
                    Nombre = d.Producto.Nombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList()
            };
        }

        public async Task PedidoEnviadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.EnPreparacion)
                throw new Exception("El pedido no se puede enviar porque no está en estado Pendiente (Estado actual: " + pedido.Estado + ")");

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoEntregadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.Enviado)
                throw new Exception($"No se puede entregar: el pedido aún figura como {pedido.Estado}.");

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoCanceladoAsync(int pedidoId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Traemos el pedido con sus detalles (Importante usar .Include)
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(dp => dp.Producto) // Traemos el producto para devolverle el stock
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

                if (pedido == null) throw new Exception("Pedido no encontrado.");

                // Validaciones de negocio
                if (pedido.Estado == EstadoPedido.Cancelado)
                    throw new Exception("El pedido ya estaba cancelado.");

                if (pedido.Estado == EstadoPedido.Entregado)
                    throw new Exception("No se puede cancelar un pedido que ya ha sido entregado.");

                if (pedido.Estado == EstadoPedido.Enviado)
                    throw new Exception("El pedido ya fue enviado, debe procesarse como devolución.");

                var debeDevolverStock = pedido.EstadoPago == EstadoPago.Aprobado || pedido.EstadoPago == EstadoPago.Reembolsado;

if (debeDevolverStock)
                {
                    var movimientosARegistrar = new List<(int ProductoId, int Cantidad, string NombreProducto)>();

                    foreach (var detalle in pedido.DetallesPedido)
                    {
                        var filasAfectadas = await _context.Productos
                            .Where(p => p.ProductoId == detalle.ProductoId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + detalle.Cantidad));

                        if (filasAfectadas == 0)
                        {
                            _logger.LogWarning("No se pudo devolver stock para producto {ProductoId}. Puede que haya sido eliminado.", detalle.ProductoId);
                            continue;
                        }

                        movimientosARegistrar.Add((detalle.ProductoId, detalle.Cantidad, detalle.Producto?.Nombre ?? $"ID {detalle.ProductoId}"));
                    }

                    foreach (var mov in movimientosARegistrar)
                    {
                        var producto = await _context.Productos.FindAsync(mov.ProductoId);
                        if (producto != null)
                        {
                            _movimientoStockCommandService.GenerarMovimiento(
                                producto,
                                mov.Cantidad,
                                TipoMovimiento.CancelacionPedido,
                                pedido.PedidoId,
                                $"Devolución por cancelación de Pedido #{pedido.PedidoId}"
                            );
                        }
                    }
                }

                pedido.Estado = EstadoPedido.Cancelado;
                pedido.EstadoPago = EstadoPago.Rechazado;
                pedido.FechaCancelado = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
}

        private async Task<bool> IntentarReservarStockAsync(Pedido pedido)
        {
            foreach (var detalle in pedido.DetallesPedido)
            {
                var filasAfectadas = await _context.Productos
                    .Where(p => p.ProductoId == detalle.ProductoId && p.Stock >= detalle.Cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - detalle.Cantidad));

                if (filasAfectadas == 0)
                {
                    var nombreProducto = detalle.Producto?.Nombre ?? $"ID {detalle.ProductoId}";
                    _logger.LogWarning("Stock insuficiente para aprobar el pedido {PedidoId}. Producto: {ProductoId} ({NombreProducto}), Cantidad: {Cantidad}",
                        pedido.PedidoId, detalle.ProductoId, nombreProducto, detalle.Cantidad);
                    return false;
                }
            }

            return true;
        }
    }
}
