using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
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
                    // La preferencia de MercadoPago suma el ítem "Envío" cuando
                    // CostoEnvio > 0: el cobro matchea el total (subtotal + envío)
                    // que valida ConfirmarPagoAsync.
                    CostoEnvio = pedidoCompleto.CostoEnvio,
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

                        // Los guards de arriba son lecturas: dos confirmaciones simultáneas
                        // (webhook + reintentos del pago) pueden pasarlas ambas, reservar el
                        // stock dos veces y encolar dos emails de "aprobado". El UPDATE
                        // condicional hace el check y la escritura en la misma instrucción;
                        // 0 filas = otro request ya cambió el pedido y el motivo se mapea
                        // desde su estado real, sin tocar stock.
                        var fechaLimiteAprobacion = DateTime.Now.Subtract(Pedido.TiempoMaximoPagoPendiente);
                        var filasAprobado = await _context.Pedidos
                            .Where(p => p.PedidoId == pedido.PedidoId
                                && p.EstadoPago == EstadoPago.Pendiente
                                && p.Estado != EstadoPedido.Cancelado
                                && p.FechaPedido > fechaLimiteAprobacion)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(p => p.EstadoPago, EstadoPago.Aprobado)
                                .SetProperty(p => p.Estado, EstadoPedido.EnPreparacion)
                                .SetProperty(p => p.FechaEnPreparacion, DateTime.Now)
                                .SetProperty(p => p.TransaccionPagoId, infoPago.TransaccionId), ct);

                        if (filasAprobado == 0)
                        {
                            return await ResolverMotivoDeCarreraAsync(pedido.PedidoId, ct);
                        }

                        // La escritura ganó la transición: este request es el responsable
                        // de la aprobación y de la reserva por ítem que sigue (atómica).
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

                // Los guards de arriba son lecturas: un doble click del admin (o una
                // aprobación simultánea con un rechazo) puede pasar dos veces el check
                // y reservar el stock dos veces. El UPDATE condicional adjudica la
                // transición una sola vez; 0 filas = otro request ya cambió el estado
                // y se informa con el mensaje del guard que corresponde a ese estado.
                var fechaLimiteAprobacion = DateTime.Now.Subtract(Pedido.TiempoMaximoPagoPendiente);
                var filasAprobado = await _context.Pedidos
                    .Where(p => p.PedidoId == pedidoId
                        && p.EstadoPago == EstadoPago.Pendiente
                        && p.Estado != EstadoPedido.Cancelado
                        && p.FechaPedido > fechaLimiteAprobacion)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.EstadoPago, EstadoPago.Aprobado)
                        .SetProperty(p => p.Estado, EstadoPedido.EnPreparacion)
                        .SetProperty(p => p.FechaEnPreparacion, DateTime.Now), ct);

                if (filasAprobado == 0)
                {
                    var estadoReal = await _context.Pedidos
                        .AsNoTracking()
                        .Where(p => p.PedidoId == pedidoId)
                        .Select(p => new { p.Estado, p.EstadoPago, p.FechaPedido })
                        .FirstOrDefaultAsync(ct);

                    if (estadoReal == null) throw new PedidoNoEncontradoException("No se encontró el pedido.");
                    if (estadoReal.Estado == EstadoPedido.Cancelado)
                        throw new PedidoNoCancelableException("El pedido está cancelado: no se puede aprobar el pago.");
                    if (estadoReal.EstadoPago == EstadoPago.Aprobado)
                        throw new PedidoNoCancelableException("El pago de este pedido ya está aprobado.");
                    if (estadoReal.FechaPedido <= DateTime.Now.Subtract(Pedido.TiempoMaximoPagoPendiente))
                        throw new PedidoNoCancelableException("El pedido venció por falta de pago. Cancelalo pedile que vuelva a comprar.");

                    throw new PedidoNoCancelableException("El pedido ya no está esperando el pago por transferencia.");
                }

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
            // Transacción: sin ella, una carrera entre rechazar y aprobar deja a la
            // vez pago aprobado, pedido cancelado y comprobante borrado de la nube.
            // El guard de estado es la escritura misma — el UPDATE condicional
            // adjudica la transición una sola vez, con rollback si algo falla antes
            // del commit.
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            string urlComprobante;
            string emailUsuario;
            string nombreCompletoUsuario;
            decimal totalCalculado;

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.DetallesPedido)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, ct);

                if (pedido == null) throw new PedidoNoEncontradoException("No se encontró el pedido.");

                if (pedido.MetodoDePagoId != MetodoPagoTransferenciaId)
                    throw new ComprobanteNoValidoException("Este pedido no paga por transferencia bancaria.");

                if (string.IsNullOrEmpty(pedido.ComprobanteUrl))
                    throw new ComprobanteNoValidoException("El pedido no tiene comprobante. Para darlo de baja sin comprobante usá el botón Cancelar.");

                // El UPDATE condicional reemplaza los guards de estado de pantalla:
                // pendiente y no cancelado pasan a rechazado + cancelado en una sola
                // instrucción, y solo un request queda como ganador; el que pierde
                // informa según el estado real que el ganador dejó, con el mensaje
                // del guard que corresponde.
                var filasRechazo = await _context.Pedidos
                    .Where(p => p.PedidoId == pedidoId
                        && p.EstadoPago == EstadoPago.Pendiente
                        && p.Estado != EstadoPedido.Cancelado)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.EstadoPago, EstadoPago.Rechazado)
                        .SetProperty(p => p.Estado, EstadoPedido.Cancelado)
                        .SetProperty(p => p.FechaCancelado, DateTime.Now), ct);

                if (filasRechazo == 0)
                {
                    var estadoReal = await _context.Pedidos
                        .AsNoTracking()
                        .Where(p => p.PedidoId == pedidoId)
                        .Select(p => new { p.Estado, p.EstadoPago })
                        .FirstOrDefaultAsync(ct);

                    if (estadoReal == null) throw new PedidoNoEncontradoException("No se encontró el pedido.");
                    if (estadoReal.Estado == EstadoPedido.Cancelado)
                        throw new PedidoNoCancelableException("El pedido ya está cancelado.");
                    if (estadoReal.EstadoPago == EstadoPago.Aprobado)
                        throw new PedidoNoCancelableException("El pago de este pedido ya está aprobado.");

                    throw new PedidoNoCancelableException("El pedido ya no está esperando verificación de comprobante.");
                }

                urlComprobante = pedido.ComprobanteUrl;
                emailUsuario = pedido.Usuario!.Email;
                nombreCompletoUsuario = string.Join(" ", new[] { pedido.Usuario.Nombre, pedido.Usuario.Apellido }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
                totalCalculado = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario) + pedido.CostoEnvio;

                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }

            // La cancelación quedó en la base: del punto en adelante los efectos
            // restantes se comportan igual que antes (la baja de Cloudinary con
            // log si falla y la notificación fuera del commit).
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

            BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPagoRechazadoAsync(
                emailUsuario,
                nombreCompletoUsuario,
                pedidoId,
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
                // El reintento arma la preferencia con el ítem de envío: mismo total
                // (subtotal + envío) que la creación, el que valida ConfirmarPagoAsync.
                CostoEnvio = pedido.CostoEnvio,
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

                // Dos cancelaciones simultáneas (doble click del admin) pueden pasar
                // dos veces la verificación de estado y devolver el stock dos veces
                // (dos movimientos de CancelacionPedido, stock inflado). El UPDATE
                // condicional garantiza que un solo request ejecuta la cancelación y
                // su devolución; el que pierde corta con el estado real.
                var filasCancelado = await _context.Pedidos
                    .Where(p => p.PedidoId == pedidoId
                        && (p.Estado == EstadoPedido.Nuevo || p.Estado == EstadoPedido.EnPreparacion))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Estado, EstadoPedido.Cancelado)
                        .SetProperty(p => p.FechaCancelado, DateTime.Now), ct);

                if (filasCancelado == 0)
                {
                    var estadoReal = await _context.Pedidos
                        .AsNoTracking()
                        .Where(p => p.PedidoId == pedidoId)
                        .Select(p => p.Estado)
                        .FirstOrDefaultAsync(ct);

                    throw new PedidoNoCancelableException(estadoReal switch
                    {
                        EstadoPedido.Cancelado => "El pedido ya estaba cancelado.",
                        EstadoPedido.Entregado => "No se puede cancelar un pedido que ya ha sido entregado.",
                        EstadoPedido.Enviado => "El pedido ya fue enviado, debe procesarse como devolución.",
                        // El caso "pedido borrado entre medias" cae en el default: no se
                        // puede describir el estado real de algo que ya no existe.
                        _ => "El pedido ya no se puede cancelar."
                    });
                }

                // El EstadoPago que decide la devolución se lee recién después del
                // UPDATE condicional, dentro de la misma transacción: si un pago
                // aprueba en paralelo, la carrera queda cerrada antes de decidir la
                // devolución — una lectura previa podría mostrar pendiente aunque
                // el pedido ya estuviera cobrado y silenciar su devolución de stock.
                var estadoPagoReal = await _context.Pedidos
                    .Where(p => p.PedidoId == pedidoId)
                    .Select(p => p.EstadoPago)
                    .FirstAsync(ct);
                var debeDevolverStock = estadoPagoReal == EstadoPago.Aprobado || estadoPagoReal == EstadoPago.Reembolsado;

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

        /// <summary>
        /// Motivo del resultado cuando el UPDATE condicional de la aprobación
        /// perdió la carrera: recarga el estado real del pedido desde la DB,
        /// porque la entidad del request perdedor quedó desactualizada. Pedido
        /// ya aprobado → YaConfirmado (un reintento repetido no repite la
        /// reserva); vencido → PedidoVencido; cualquier otro estado →
        /// EstadoIncoherente.
        /// </summary>
        private async Task<ResultadoPago> ResolverMotivoDeCarreraAsync(int pedidoId, CancellationToken ct)
        {
            var estadoReal = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new { p.Estado, p.EstadoPago, p.FechaPedido })
                .FirstOrDefaultAsync(ct);

            if (estadoReal == null)
            {
                _logger.LogWarning("El pedido {PedidoId} desapareció durante la carrera de confirmación.", pedidoId);
                return ResultadoPago.Fallo(MotivoPago.PedidoNoEncontrado);
            }

            if (estadoReal.EstadoPago == EstadoPago.Aprobado)
                return ResultadoPago.ExitoYaConfirmado();

            if (estadoReal.FechaPedido <= DateTime.Now.Subtract(Pedido.TiempoMaximoPagoPendiente))
                return ResultadoPago.Fallo(MotivoPago.PedidoVencido);

            _logger.LogInformation(
                "Llegó una aprobación tardía al pedido {PedidoId} en estado {EstadoPago}/{Estado}: se descarta (EstadoIncoherente).",
                pedidoId, estadoReal.EstadoPago, estadoReal.Estado);
            return ResultadoPago.Fallo(MotivoPago.EstadoIncoherente);
        }
    }
}
