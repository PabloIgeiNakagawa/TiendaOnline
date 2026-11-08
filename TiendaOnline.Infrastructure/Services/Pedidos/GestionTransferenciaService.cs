using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class GestionTransferenciaService : IGestionTransferenciaService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;
        private readonly IImagenService _imagenService;
        private readonly IAppSettingsService _appSettings;
        private readonly ILogger<GestionTransferenciaService> _logger;

        // Id de "Transferencia Bancaria" en la tabla MetodoDePago (ver migration MetodoDePagoYCambiosPedido)
        private const int MetodoPagoTransferenciaId = 2;

        public GestionTransferenciaService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService, IImagenService imagenService, IAppSettingsService appSettings, ILogger<GestionTransferenciaService> logger)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
            _imagenService = imagenService;
            _appSettings = appSettings;
            _logger = logger;
        }

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
                var reservaStockExitosa = await ReservaDeStock.ReservarAsync(_context, _logger, pedido, ct);
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
                await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
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
                await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
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
    }
}
