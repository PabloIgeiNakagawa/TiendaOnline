using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Admin.MovimientosStock
{
    public class MovimientoStockService : IMovimientoStockService
    {
        private readonly TiendaContext _context;

        public MovimientoStockService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<MovimientosDto>> ObtenerMovimientosPaginadosAsync(MovimientoFiltrosDto filtros)
        {
            var query = _context.MovimientosStock
                .AsNoTracking()
                .Include(m => m.Producto)
                .Include(m => m.Pedido)
                .AsQueryable();

            // --- Filtros ---

            // Búsqueda por nombre de producto
            if (!string.IsNullOrEmpty(filtros.Busqueda))
            {
                query = query.Where(m => m.Producto.Nombre.Contains(filtros.Busqueda));
            }

            // Filtro por Tipo de Movimiento (Enum)
            if (filtros.TipoMovimientoId.HasValue)
            {
                query = query.Where(m => (int)m.Tipo == filtros.TipoMovimientoId.Value);
            }

            // Filtro por Rango de Fechas
            if (filtros.Desde.HasValue)
            {
                query = query.Where(m => m.Fecha >= filtros.Desde.Value);
            }

            if (filtros.Hasta.HasValue)
            {
                // Le sumamos un día o usamos la lógica de "menor a mañana" para incluir todo el día 'hasta'
                var fechaHasta = filtros.Hasta.Value.Date.AddDays(1);
                query = query.Where(m => m.Fecha < fechaHasta);
            }

            // --- Paginación ---
            int totalElementos = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.Fecha)
                .Skip((filtros.Pagina - 1) * filtros.RegistrosPorPagina)
                .Take(filtros.RegistrosPorPagina)
                .Select(m => new MovimientosDto
                {
                    MovimientoId = m.MovimientoStockId,
                    ProductoNombre = m.Producto.Nombre,
                    ImagenUrl = m.Producto.ImagenUrl,
                    Cantidad = m.Cantidad,
                    Tipo = m.Tipo.ToString(),
                    Fecha = m.Fecha,
                    Observaciones = m.Observaciones,
                    PedidoId = m.PedidoId
                })
                .ToListAsync();

            return new PagedResult<MovimientosDto>(items, totalElementos, filtros.Pagina, filtros.RegistrosPorPagina);
        }

        public async Task<IEnumerable<TipoMovimientoDTO>> ObtenerTiposMovimientoAsync()
        {
            // Obtenemos todos los valores del Enum 'TipoMovimiento'
            var tipos = Enum.GetValues(typeof(TipoMovimiento))
                            .Cast<TipoMovimiento>()
                            .Select(t => new TipoMovimientoDTO
                            {
                                Id = (int)t,
                                Nombre = t.ToString()
                            })
                            .ToList();

            return await Task.FromResult(tipos);
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

        // OBTENER HISTORIAL (Para reportes del Admin)
        public async Task<List<MovimientoStock>> ObtenerHistorialPorProductoAsync(int productoId)
        {
            return await _context.MovimientosStock
                .Include(m => m.Pedido)
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
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