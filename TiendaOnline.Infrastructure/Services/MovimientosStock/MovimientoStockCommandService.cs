using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.MovimientosStock
{
    public class MovimientoStockCommandService : IMovimientoStockCommandService
    {
        private readonly TiendaContext _context;
        private readonly ILogger<MovimientoStockCommandService> _logger;

        public MovimientoStockCommandService(TiendaContext context, ILogger<MovimientoStockCommandService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ENTRADA DE STOCK (Compras a proveedores)
        public async Task RegistrarEntradaAsync(RegistroStockDto dto, CancellationToken ct = default)
        {
            var producto = await _context.Productos.FindAsync(dto.ProductoId, ct);
            if (producto == null) throw new Exception("Producto no encontrado");

            // Solo entradas positivas: una cantidad no positiva inventaría o deshace stock.
            if (dto.Cantidad <= 0)
                throw new MovimientoStockNoValidoException("La cantidad de la entrada debe ser positiva.");

            producto.Stock += dto.Cantidad;

            GenerarMovimiento(producto, dto.Cantidad, TipoMovimiento.EntradaStock, null, dto.Observaciones ?? "Entrada de mercadería");

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Entrada de stock: {Cantidad:+0;-0;0} para producto {ProductoId} (stock final {StockFinal})", dto.Cantidad, dto.ProductoId, producto.Stock);
        }

        // AJUSTE MANUAL (Roturas, pérdidas, hallazgos)
        public async Task RegistrarAjusteManualAsync(AjusteManualDto dto, CancellationToken ct = default)
        {
            var producto = await _context.Productos.FindAsync(dto.ProductoId, ct);
            if (producto == null) throw new Exception("Producto no encontrado");

            // La cantidad en el DTO puede ser negativa (ej: -2 por rotura), pero el
            // resultado tiene el piso en 0: el inventario no queda negativo.
            if (producto.Stock + dto.Cantidad < 0)
                throw new MovimientoStockNoValidoException($"El ajuste dejaría el stock en negativo (stock actual: {producto.Stock}). Revisá la cantidad.");

            producto.Stock += dto.Cantidad;

            GenerarMovimiento(producto, dto.Cantidad, TipoMovimiento.AjusteManual, null, dto.Observaciones);

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Ajuste manual de stock: {Cantidad:+0;-0;0} para producto {ProductoId} (stock final {StockFinal})", dto.Cantidad, dto.ProductoId, producto.Stock);
        }

        // DEVOLUCIÓN (Cliente devuelve producto de un pedido)
        public async Task RegistrarDevolucionAsync(DevolucionStockDto dto, CancellationToken ct = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var producto = await _context.Productos.FindAsync(dto.ProductoId, ct);
                if (producto == null) throw new Exception("Producto no encontrado");

                // Validación de integridad: ¿El producto estaba en ese pedido (y devolvé solo hasta lo comprado)?
                var cantidadComprada = await _context.DetallesPedido
                    .Where(dp => dp.PedidoId == dto.PedidoId && dp.ProductoId == dto.ProductoId)
                    .Select(dp => dp.Cantidad)
                    .FirstOrDefaultAsync(ct);

                if (cantidadComprada == 0)
                    throw new MovimientoStockNoValidoException("El producto no pertenece al pedido indicado.");

                // La devolución solo suma stock y hasta lo comprado del pedido: más
                // cantidad inventaría unidades que nunca entraron al sistema.
                if (dto.Cantidad <= 0)
                    throw new MovimientoStockNoValidoException("La devolución debe sumar stock, no restarlo.");

                if (dto.Cantidad > cantidadComprada)
                    throw new MovimientoStockNoValidoException($"No se puede devolver más que las {cantidadComprada} unidades compradas en el pedido.");

                producto.Stock += dto.Cantidad;

                GenerarMovimiento(
                    producto,
                    dto.Cantidad,
                    TipoMovimiento.Devolucion,
                    dto.PedidoId,
                    $"Devolución: {dto.Observaciones}"
                );

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                _logger.LogInformation("Devolución de stock: {Cantidad:+0;-0;0} del producto {ProductoId} (pedido {PedidoId}, stock final {StockFinal})", dto.Cantidad, dto.ProductoId, dto.PedidoId, producto.Stock);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        // MÉTODO PARA OTROS SERVICES (PedidoService usa este)
        // No hace SaveChanges para permitir que el otro servicio maneje la transacción.
        public void GenerarMovimiento(Producto producto, int cantidad, TipoMovimiento tipo, int? pedidoId, string observaciones)
        {
            var movimiento = new MovimientoStock
            {
                Producto = producto,
                ProductoId = producto.ProductoId,
                Cantidad = cantidad,
                Tipo = tipo,
                Fecha = DateTime.Now,
                Observaciones = observaciones,
                PedidoId = pedidoId
            };

            _context.MovimientosStock.Add(movimiento);
        }
    }
}
