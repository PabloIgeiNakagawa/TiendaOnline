using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class EstadosPedidoService : IEstadosPedidoService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;
        private readonly ILogger<EstadosPedidoService> _logger;

        public EstadosPedidoService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService, ILogger<EstadosPedidoService> logger)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
            _logger = logger;
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
            // La entrega la marca el reparto (rol Repartidor) o el admin desde el panel;
            // ambos comparten este mismo caso de uso con los mismos guards.
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
                await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
                throw;
            }
        }
    }
}
