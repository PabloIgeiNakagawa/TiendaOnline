using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.MovimientosStock.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.MovimientosStock
{
    public class MovimientoStockQueryService : IMovimientoStockQueryService
    {
        private readonly TiendaContext _context;

        public MovimientoStockQueryService(TiendaContext context)
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

        public async Task<IEnumerable<TipoMovimientoDto>> ObtenerTiposMovimientoAsync()
        {
            // Obtenemos todos los valores del Enum 'TipoMovimiento'
            var tipos = Enum.GetValues(typeof(TipoMovimiento))
                            .Cast<TipoMovimiento>()
                            .Select(t => new TipoMovimientoDto
                            {
                                Id = (int)t,
                                Nombre = t.ToString()
                            })
                            .ToList();

            return await Task.FromResult(tipos);
        }

        // OBTENER HISTORIAL (Para reportes del Admin)
        public async Task<List<MovimientosDto>> ObtenerHistorialPorProductoAsync(int productoId)
        {
            var movimientos = await _context.MovimientosStock
                .AsNoTracking()
                .Include(m => m.Pedido)
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return movimientos
                .Select(m => new MovimientosDto
                {
                    MovimientoId = m.MovimientoStockId,
                    ProductoNombre = m.Producto?.Nombre,
                    ImagenUrl = m.Producto?.ImagenUrl,
                    Cantidad = m.Cantidad,
                    Tipo = m.Tipo.ToString(),
                    Fecha = m.Fecha,
                    Observaciones = m.Observaciones,
                    PedidoId = m.PedidoId
                })
                .ToList();
        }
    }
}
