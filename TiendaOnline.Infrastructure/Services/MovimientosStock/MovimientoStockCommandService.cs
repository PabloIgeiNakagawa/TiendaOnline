using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.MovimientosStock
{
    public class MovimientoStockCommandService : IMovimientoStockCommandService
    {
        private readonly TiendaContext _context;

        public MovimientoStockCommandService(TiendaContext context)
        {
            _context = context;
        }

        // ENTRADA DE STOCK (Compras a proveedores)
        public async Task RegistrarEntradaAsync(RegistroStockDto dto)
        {
            var producto = await _context.Productos.FindAsync(dto.ProductoId);
            if (producto == null) throw new Exception("Producto no encontrado");

            producto.Stock += dto.Cantidad;

            GenerarMovimiento(producto, dto.Cantidad, TipoMovimiento.EntradaStock, null, dto.Observaciones ?? "Entrada de mercadería");

            await _context.SaveChangesAsync();
        }

        // AJUSTE MANUAL (Roturas, pérdidas, hallazgos)
        public async Task RegistrarAjusteManualAsync(AjusteManualDto dto)
        {
            var producto = await _context.Productos.FindAsync(dto.ProductoId);
            if (producto == null) throw new Exception("Producto no encontrado");

            // La cantidad en el DTO puede ser negativa (ej: -2 por rotura)
            producto.Stock += dto.Cantidad;

            GenerarMovimiento(producto, dto.Cantidad, TipoMovimiento.AjusteManual, null, dto.Observaciones);

            await _context.SaveChangesAsync();
        }

        // DEVOLUCIÓN (Cliente devuelve producto de un pedido)
        public async Task RegistrarDevolucionAsync(DevolucionStockDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FindAsync(dto.ProductoId);
                if (producto == null) throw new Exception("Producto no encontrado");

                // Validación de integridad: ¿El producto estaba en ese pedido?
                var detalleOriginal = await _context.DetallesPedido
                    .AnyAsync(dp => dp.PedidoId == dto.PedidoId && dp.ProductoId == dto.ProductoId);

                if (!detalleOriginal)
                    throw new Exception("El producto no pertenece al pedido indicado.");

                producto.Stock += dto.Cantidad;

                GenerarMovimiento(
                    producto,
                    dto.Cantidad,
                    TipoMovimiento.Devolucion,
                    dto.PedidoId,
                    $"Devolución: {dto.Observaciones}"
                );

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
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
