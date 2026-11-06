using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
public class PedidoCommandService : IPedidoCommandService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;
        private readonly IImagenService _imagenService;
        private readonly IAppSettingsService _appSettings;
        private readonly IOfertaQueryService _ofertaQueryService;
        private readonly ILogger<PedidoCommandService> _logger;

        // Id de "Transferencia Bancaria" en la tabla MetodoDePago (ver migration MetodoDePagoYCambiosPedido)
        private const int MetodoPagoTransferenciaId = 2;

        public PedidoCommandService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService, IImagenService imagenService, IAppSettingsService appSettings, IOfertaQueryService ofertaQueryService, ILogger<PedidoCommandService> logger)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
            _imagenService = imagenService;
            _appSettings = appSettings;
            _ofertaQueryService = ofertaQueryService;
            _logger = logger;
        }

        public async Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto, CancellationToken ct = default)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("El pedido no tiene ítems.");

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

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
                    var producto = await _context.Productos.FindAsync(itemDto.ProductoId, ct);
                    if (producto == null)
                        throw new Exception($"No se encontró el producto con ID {itemDto.ProductoId}.");

                    // Precio efectivo del cobro: misma regla que vio el cliente
                    // en carrito/checkout — mejor oferta vigente ?? precio de lista.
                    var oferta = await _ofertaQueryService.ObtenerOfertaVigenteAsync(producto.ProductoId, ct);

                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = itemDto.Cantidad,
                        PrecioUnitario = oferta?.PrecioOferta ?? producto.Precio,
                        Producto = producto // Importante para que el PaymentService tenga el nombre
                    });
                }

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // RECARGAMOS EL PEDIDO CON SUS INCLUDES
                var pedidoCompleto = await _context.Pedidos
                    .Include(p => p.Usuario) // Traemos los datos del usuario (Email)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto) // Traemos los datos del producto (Nombre)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedido.PedidoId, ct);

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
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<ResultadoPago> ConfirmarPagoAsync(InfoPagoDto infoPago, CancellationToken ct = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.PedidoId == infoPago.PedidoId, ct);

                // Idempotencia y existencia: Si no existe o ya está pagado, cortamos acá
                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el pedido {PedidoId} para confirmar pago.", infoPago.PedidoId);
                    return ResultadoPago.Fallo(MotivoPago.PedidoNoEncontrado);
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
                    return ResultadoPago.Fallo(MotivoPago.MontoNoCoincide);
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
                            return ResultadoPago.ExitoYaConfirmado();
                        }
                        if (pedidoVencido)
                        {
                            _logger.LogWarning("El pedido {PedidoId} ya venció. Ignorando aprobación tardía. Transacción: {TransaccionId}",
                                infoPago.PedidoId, infoPago.TransaccionId);
                            return ResultadoPago.Fallo(MotivoPago.PedidoVencido);
                        }

                        if (pedido.Estado == EstadoPedido.Cancelado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba cancelado. Ignorando aprobación tardía.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando aprobación tardía.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        var reservaStockExitosa = await IntentarReservarStockAsync(pedido, ct);
                        if (!reservaStockExitosa)
                        {
                            await transaction.RollbackAsync(ct);
                            return ResultadoPago.Fallo(MotivoPago.StockInsuficiente);
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
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago == EstadoPago.Rechazado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba rechazado. Ignorando.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando rechazo posterior.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago == EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba aprobado. Ignorando rechazo posterior.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
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
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago == EstadoPago.Reembolsado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba reembolsado. Ignorando cancelación posterior.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago == EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} ya estaba aprobado. Ignorando cancelación posterior.", infoPago.PedidoId);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
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
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        if (pedido.EstadoPago != EstadoPago.Aprobado)
                        {
                            _logger.LogInformation("El pedido {PedidoId} no estaba aprobado. Ignorando reembolso para estado {EstadoPago}.", infoPago.PedidoId, pedido.EstadoPago);
                            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
                        }

                        pedido.EstadoPago = EstadoPago.Reembolsado;
                        pedido.TransaccionPagoId = infoPago.TransaccionId;
                        resultadoEfectivo = true;
                        tipoEmailAEnviar = "reembolsado";
                        break;
                }

                if (!resultadoEfectivo)
                {
                    // Estado de MP no mapeado a una transición (o señal vacía): no se toca nada.
                    return ResultadoPago.Fallo(MotivoPago.EstadoNoManejado);
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // Emails: se encolan recién DESPUÉS del commit.
                // Si el commit falla, el cliente no debe recibir "pago aprobado"
                // por un pedido que quedó sin confirmar en la base.
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

                _logger.LogInformation("Pedido {PedidoId} procesado con estado MP: {Estado}. Transacción: {TransaccionId}",
                    infoPago.PedidoId, estadoNormalizado, infoPago.TransaccionId);
                return ResultadoPago.Exito();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error al procesar el pago del pedido {PedidoId}", infoPago.PedidoId);
                throw;
            }
        }

        // ----- Comprobante de pago por transferencia bancaria -----

        public async Task<string> GuardarComprobanteAsync(int pedidoId, Stream archivo, string nombreArchivo, CancellationToken ct = default)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId, ct);
            if (pedido == null) throw new PedidoNoEncontradoException("No se encontró el pedido.");

            if (pedido.MetodoDePagoId != MetodoPagoTransferenciaId)
                throw new ComprobanteNoValidoException("Este pedido no paga por transferencia bancaria.");

            if (pedido.Estado != EstadoPedido.Nuevo || pedido.EstadoPago != EstadoPago.Pendiente)
                throw new ComprobanteNoValidoException("El pedido ya no está esperando el comprobante.");

            if (!string.IsNullOrEmpty(pedido.ComprobanteUrl))
                throw new ComprobanteNoValidoException("Ya se subió un comprobante para este pedido.");

            // Primero Cloudinary: si la subida falla no queda ningún cambio en la base.
            var url = await _imagenService.SubirComprobanteAsync(archivo, nombreArchivo, ct);
            if (string.IsNullOrEmpty(url))
                throw new ComprobanteNoValidoException("No se pudo subir el comprobante. Verificá el archivo e intentá de nuevo.");

            pedido.ComprobanteUrl = url;
            pedido.FechaComprobante = DateTime.Now;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Comprobante subido para el pedido {PedidoId}", pedidoId);

            // Aviso al dueño después del save; la dirección vive en AppSettings (Pagos:EmailNotificaciones).
            var emailNotificaciones = _appSettings.GetValue("Pagos:EmailNotificaciones");
            if (!string.IsNullOrWhiteSpace(emailNotificaciones))
            {
                BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailComprobanteRecibidoAsync(emailNotificaciones, pedidoId));
            }
            else
            {
                _logger.LogWarning("Comprobante del pedido {PedidoId} recibido pero Pagos:EmailNotificaciones no está configurada: no se envió aviso.", pedidoId);
            }

            return url;
        }

        public async Task AprobarPagoTransferenciaAsync(int pedidoId, CancellationToken ct = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, ct);

                if (pedido == null) throw new PedidoNoEncontradoException("No se encontró el pedido.");

                if (pedido.MetodoDePagoId != MetodoPagoTransferenciaId)
                    throw new ComprobanteNoValidoException("Este pedido no paga por transferencia bancaria.");

                if (pedido.EstadoPago == EstadoPago.Aprobado)
                    throw new PedidoNoCancelableException("El pago de este pedido ya está aprobado.");

                if (pedido.Estado == EstadoPedido.Cancelado)
                    throw new PedidoNoCancelableException("El pedido está cancelado: no se puede aprobar el pago.");

                if (pedido.EstaVencido())
                    throw new PedidoNoCancelableException("El pedido venció por falta de pago. Cancelalo pedile que vuelva a comprar.");

                if (string.IsNullOrEmpty(pedido.ComprobanteUrl))
                    throw new ComprobanteNoValidoException("El pedido no tiene comprobante adjuntado todavía.");

                // Misma maquinaria que la aprobación de Mercado Pago: reserva + movimientos de salida.
                var reservaStockExitosa = await IntentarReservarStockAsync(pedido, ct);
                if (!reservaStockExitosa)
                    throw new StockInsuficienteException("Stock insuficiente para aprobar el pedido. Revisá el stock y avisale al cliente.");

                foreach (var detalle in pedido.DetallesPedido.Where(d => d.Producto != null))
                {
                    _movimientoStockCommandService.GenerarMovimiento(
                        detalle.Producto!,
                        -detalle.Cantidad,
                        TipoMovimiento.SalidaVenta,
                        pedido.PedidoId,
                        $"Venta confirmada por transferencia bancaria en Pedido #{pedido.PedidoId}"
                    );
                }

                pedido.EstadoPago = EstadoPago.Aprobado;
                pedido.Estado = EstadoPedido.EnPreparacion;
                pedido.FechaEnPreparacion = DateTime.Now;

                decimal totalCalculado = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario) + pedido.CostoEnvio;
                var nombreCompletoUsuario = string.Join(" ", new[] { pedido.Usuario!.Nombre, pedido.Usuario.Apellido }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // Email recién después del commit, igual que en Mercado Pago.
                BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoExitosoAsync(
                    pedido.Usuario!.Email,
                    nombreCompletoUsuario,
                    pedido.PedidoId,
                    totalCalculado));

                _logger.LogInformation("Pago por transferencia aprobado para el pedido {PedidoId}: stock reservado y pedido en preparación.", pedidoId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "No se pudo aprobar el pago por transferencia del pedido {PedidoId}", pedidoId);
                throw;
            }
        }

        public async Task RechazarComprobanteTransferenciaAsync(int pedidoId, CancellationToken ct = default)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, ct);

            if (pedido == null) throw new PedidoNoEncontradoException("No se encontró el pedido.");

            if (pedido.MetodoDePagoId != MetodoPagoTransferenciaId)
                throw new ComprobanteNoValidoException("Este pedido no paga por transferencia bancaria.");

            if (pedido.Estado == EstadoPedido.Cancelado)
                throw new PedidoNoCancelableException("El pedido ya está cancelado.");

            if (pedido.EstadoPago != EstadoPago.Pendiente)
                throw new PedidoNoCancelableException("El pedido ya no está esperando verificación de comprobante.");

            if (string.IsNullOrEmpty(pedido.ComprobanteUrl))
                throw new ComprobanteNoValidoException("El pedido no tiene comprobante. Para darlo de baja sin comprobante usá el botón Cancelar.");

            pedido.EstadoPago = EstadoPago.Rechazado;
            pedido.Estado = EstadoPedido.Cancelado;
            pedido.FechaCancelado = DateTime.Now;

            var urlComprobante = pedido.ComprobanteUrl;
            await _context.SaveChangesAsync(ct);

            // Borro el archivo de Cloudinary al salir: el rechazo es el fin de la vida del comprobante.
            // La baja fallida no cancela la notificación al cliente.
            var publicId = _imagenService.ExtraerPublicIdDesdeUrl(urlComprobante);
            if (!string.IsNullOrEmpty(publicId))
            {
                try
                {
                    await _imagenService.BorrarImagenAsync(publicId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo borrar el comprobante de Cloudinary para el pedido {PedidoId}", pedidoId);
                }
            }

            var nombreCompletoUsuario = string.Join(" ", new[] { pedido.Usuario!.Nombre, pedido.Usuario.Apellido }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            decimal totalCalculado = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario) + pedido.CostoEnvio;

            BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoRechazadoAsync(
                pedido.Usuario!.Email,
                nombreCompletoUsuario,
                pedido.PedidoId,
                totalCalculado));

            _logger.LogInformation("Comprobante rechazado y pedido {PedidoId} cancelado: el cliente fue notificado.", pedidoId);
        }

        public async Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId, CancellationToken ct = default)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, ct);

            if (pedido == null) return null;

            // Solo permitimos reintentar si el pedido sigue pendiente
            if (pedido.EstadoPago == EstadoPago.Aprobado)
                throw new PedidoNoCancelableException("Este pedido ya ha sido pagado.");

            if (pedido.Estado == EstadoPedido.Cancelado || pedido.EstaVencido())
                throw new PedidoNoCancelableException("Este pedido ya no admite nuevos intentos de pago.");

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

        public async Task PedidoEnviadoAsync(int pedidoId, CancellationToken ct = default)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId, ct);
            if (pedido == null) throw new PedidoNoEncontradoException("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.EnPreparacion)
                throw new PedidoNoCancelableException("El pedido no se puede enviar porque no está en preparación (Estado actual: " + pedido.Estado + ").");

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;

            await _context.SaveChangesAsync(ct);
        }

        public async Task PedidoEntregadoAsync(int pedidoId, CancellationToken ct = default)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId, ct);
            if (pedido == null) throw new PedidoNoEncontradoException("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.Enviado)
                throw new PedidoNoCancelableException($"No se puede entregar: el pedido aún figura como {pedido.Estado}.");

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;

            await _context.SaveChangesAsync(ct);
        }

        public async Task PedidoCanceladoAsync(int pedidoId, CancellationToken ct = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                // Traemos el pedido con sus detalles (Importante usar .Include)
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(dp => dp.Producto) // Traemos el producto para devolverle el stock
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, ct);

                if (pedido == null) throw new PedidoNoEncontradoException("Pedido no encontrado.");

                // Validaciones de negocio
                if (pedido.Estado == EstadoPedido.Cancelado)
                    throw new PedidoNoCancelableException("El pedido ya estaba cancelado.");

                if (pedido.Estado == EstadoPedido.Entregado)
                    throw new PedidoNoCancelableException("No se puede cancelar un pedido que ya ha sido entregado.");

                if (pedido.Estado == EstadoPedido.Enviado)
                    throw new PedidoNoCancelableException("El pedido ya fue enviado, debe procesarse como devolución.");

                var debeDevolverStock = pedido.EstadoPago == EstadoPago.Aprobado || pedido.EstadoPago == EstadoPago.Reembolsado;

if (debeDevolverStock)
                {
                    var movimientosARegistrar = new List<(int ProductoId, int Cantidad, string NombreProducto)>();

                    foreach (var detalle in pedido.DetallesPedido)
                    {
                        var filasAfectadas = await _context.Productos
                            .Where(p => p.ProductoId == detalle.ProductoId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + detalle.Cantidad), ct);

                        if (filasAfectadas == 0)
                        {
                            _logger.LogWarning("No se pudo devolver stock para producto {ProductoId}. Puede que haya sido eliminado.", detalle.ProductoId);
                            continue;
                        }

                        movimientosARegistrar.Add((detalle.ProductoId, detalle.Cantidad, detalle.Producto?.Nombre ?? $"ID {detalle.ProductoId}"));
                    }

                    foreach (var mov in movimientosARegistrar)
                    {
                        var producto = await _context.Productos.FindAsync(mov.ProductoId, ct);
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

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
}

        private async Task<bool> IntentarReservarStockAsync(Pedido pedido, CancellationToken ct)
        {
            foreach (var detalle in pedido.DetallesPedido)
            {
                var filasAfectadas = await _context.Productos
                    .Where(p => p.ProductoId == detalle.ProductoId && p.Stock >= detalle.Cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - detalle.Cantidad), ct);

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
