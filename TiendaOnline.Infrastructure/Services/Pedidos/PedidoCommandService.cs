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
        private readonly IEmailService _emailService;
        private readonly ILogger<PedidoCommandService> _logger;

        public PedidoCommandService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService, IEmailService emailService, ILogger<PedidoCommandService> logger)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
            _emailService = emailService;
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
                    // 1. Descontamos stock de forma atómica en la base de datos
                    // Solo se restará si el stock actual es mayor o igual a la cantidad solicitada
                    var filasAfectadas = await _context.Productos
                        .Where(p => p.ProductoId == itemDto.ProductoId && p.Stock >= itemDto.Cantidad)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - itemDto.Cantidad));

                    if (filasAfectadas == 0)
                    {
                        // Buscamos el nombre para dar un error descriptivo
                        var productoError = await _context.Productos.FindAsync(itemDto.ProductoId);
                        throw new Exception($"Lo sentimos, ya no queda stock suficiente para {productoError?.Nombre ?? "el producto " + itemDto.ProductoId}.");
                    }

                    // 2. Buscamos el producto para obtener datos de visualización (precio, nombre)
                    var producto = await _context.Productos.FindAsync(itemDto.ProductoId);

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

                // 2. Generamos movimientos de stock (solo lógica de auditoría)
                foreach (var detalle in pedido.DetallesPedido)
                {
                    _movimientoStockCommandService.GenerarMovimiento(
                        detalle.Producto,
                        -detalle.Cantidad,
                        TipoMovimiento.SalidaVenta,
                        pedido.PedidoId,
                        $"Reserva por Pedido #{pedido.PedidoId}"
                    );
                }

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

                if (pedido.EstadoPago == EstadoPago.Aprobado)
                {
                    _logger.LogInformation("El pedido {PedidoId} ya estaba aprobado. Ignorando.", infoPago.PedidoId);
                    return false;
                }

                if (pedido.EstadoPago == EstadoPago.Rechazado)
                {
                    _logger.LogInformation("El pedido {PedidoId} ya estaba rechazado. Ignorando.", infoPago.PedidoId);
                    return false;
                }

                // Calculamos el total real (Subtotal + Envío) con precisión decimal
                decimal subtotal = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
                decimal totalCalculado = subtotal + pedido.CostoEnvio;

                // Seguridad: Comparamos el monto pagado en MP vs el total en nuestra DB
                // Usamos una diferencia menor a 0.01m por temas de redondeo
                if (Math.Abs(infoPago.MontoPagado - totalCalculado) > 0.01m)
                {
                    _logger.LogWarning("Mismatch de montos en pedido {PedidoId}. MP: {MontoMP}, Sistema: {MontoSistema} (Subtotal: {Subtotal}, Envío: {Envio}), TransaccionId: {TransaccionId}",
                        infoPago.PedidoId, infoPago.MontoPagado, totalCalculado, subtotal, pedido.CostoEnvio, infoPago.TransaccionId);
                    return false;
                }

                bool resultadoEfectivo = false;

                switch (infoPago.Estado)
                {
                    case "approved":
                        pedido.EstadoPago = EstadoPago.Aprobado;
                        pedido.Estado = EstadoPedido.EnPreparacion;
                        pedido.FechaEnPreparacion = DateTime.Now;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        break;

                    case "rejected":
                    case "declined":
                        pedido.EstadoPago = EstadoPago.Rechazado;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        break;

                    case "cancelled":
                        pedido.EstadoPago = EstadoPago.Rechazado;
                        pedido.Estado = EstadoPedido.Cancelado;
                        pedido.FechaCancelado = DateTime.Now;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;

                        // Devolver stock (Ya cargado eficientemente mediante ThenInclude)
                        foreach (var detalle in pedido.DetallesPedido)
                        {
                            if (detalle.Producto != null)
                            {
                                detalle.Producto.Stock += detalle.Cantidad;
                                _movimientoStockCommandService.GenerarMovimiento(
                                    detalle.Producto,
                                    detalle.Cantidad,
                                    TipoMovimiento.CancelacionPedido,
                                    pedido.PedidoId,
                                    $"Devolución por cancelación de pago en Pedido #{pedido.PedidoId}"
                                );
                            }
                        }
                        resultadoEfectivo = true;
                        break;

                    case "refunded":
                    case "charged_back":
                        pedido.EstadoPago = EstadoPago.Reembolsado;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        break;
                }

                if (resultadoEfectivo)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // SOLO ENCOLAR EMAIL SI SE APROBÓ EL PAGO (Post-Commit)
                    if (infoPago.Estado == "approved")
                    {
                        BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoExitosoAsync(
                            pedido.Usuario.Email,
                            $"{pedido.Usuario.Nombre} {pedido.Usuario.Apellido}",
                            pedido.PedidoId,
                            totalCalculado
                        ));
                    }

                    _logger.LogInformation("Pedido {PedidoId} procesado con estado MP: {Estado}. Transacción: {TransaccionId}",
                        infoPago.PedidoId, infoPago.Estado, infoPago.TransaccionId);
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

                // Devolver el stock por cada detalle
                foreach (var detalle in pedido.DetallesPedido)
                {
                    // Sumamos lo que antes restamos
                    detalle.Producto.Stock += detalle.Cantidad;

                    // Registramos el movimiento de entrada por cancelación
                    // Cantidad positiva porque entra de nuevo
                    _movimientoStockCommandService.GenerarMovimiento(
                        detalle.Producto,
                        detalle.Cantidad,
                        TipoMovimiento.CancelacionPedido,
                        pedido.PedidoId,
                        $"Devolución por cancelación de Pedido #{pedido.PedidoId}"
                    );
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
    }
}
