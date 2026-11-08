using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Pedidos.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class ConfirmacionPagoService : IConfirmacionPagoService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;
        private readonly ILogger<ConfirmacionPagoService> _logger;

        public ConfirmacionPagoService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService, ILogger<ConfirmacionPagoService> logger)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
            _logger = logger;
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
                        var reservaStockExitosa = await ReservaDeStock.ReservarAsync(_context, _logger, pedido, ct);
                        if (!reservaStockExitosa)
                        {
                            await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
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
                await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
                _logger.LogError(ex, "Error al procesar el pago del pedido {PedidoId}", infoPago.PedidoId);
                throw;
            }
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
