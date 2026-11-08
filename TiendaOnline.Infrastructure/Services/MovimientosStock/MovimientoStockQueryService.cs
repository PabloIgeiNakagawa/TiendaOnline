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

        public async Task<PagedResult<MovimientosDto>> ObtenerMovimientosPaginadosAsync(MovimientoFiltrosDto filtros, CancellationToken ct = default)
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
                // La columna Tipo es texto en la BD (estados como texto): la
                // comparación es enum↔enum con la conversión del modelo; un cast
                // SQL sobre el texto no devuelve filas (y rompería en SQL Server).
                var tipoFiltro = (Domain.Entities.TipoMovimiento)filtros.TipoMovimientoId.Value;
                query = query.Where(m => m.Tipo == tipoFiltro);
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
            int totalElementos = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(m => m.Fecha)
                .Skip((filtros.Pagina - 1) * filtros.RegistrosPorPagina)
                .Take(filtros.RegistrosPorPagina)
                .ToListAsync(ct);

            var resultado = items.Select(m => new MovimientosDto
            {
                MovimientoId = m.MovimientoStockId,
                ProductoNombre = m.Producto.Nombre,
                ImagenUrl = m.Producto.ImagenUrl,
                Cantidad = m.Cantidad,
                TipoMovimientoId = (int)Enum.Parse(typeof(Domain.Entities.TipoMovimiento), m.Tipo.ToString()),
                Fecha = m.Fecha,
                Observaciones = m.Observaciones,
                PedidoId = m.PedidoId
            }).ToList();

            return new PagedResult<MovimientosDto>(resultado, totalElementos, filtros.Pagina, filtros.RegistrosPorPagina);
        }

        public async Task<IEnumerable<TipoMovimientoDto>> ObtenerTiposMovimientoAsync(CancellationToken ct = default)
        {
            // Obtenemos todos los valores del Enum 'TipoMovimiento'
            var tipos = Enum.GetValues(typeof(Domain.Entities.TipoMovimiento))
                            .Cast<Domain.Entities.TipoMovimiento>()
                            .Select(t => new TipoMovimientoDto
                            {
                                Id = (int)t,
                                Nombre = t.ToString()
                            })
                            .ToList();

            return await Task.FromResult(tipos);
        }

        // OBTENER HISTORIAL (Para reportes del Admin)
        public async Task<List<MovimientosDto>> ObtenerHistorialPorProductoAsync(int productoId, CancellationToken ct = default)
        {
            var movimientos = await _context.MovimientosStock
                .AsNoTracking()
                .Include(m => m.Pedido)
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync(ct);

            return movimientos
                .Select(m => new MovimientosDto
                {
                    MovimientoId = m.MovimientoStockId,
                    ProductoNombre = m.Producto?.Nombre,
                    ImagenUrl = m.Producto?.ImagenUrl,
                    Cantidad = m.Cantidad,
                    TipoMovimientoId = (int)Enum.Parse(typeof(Domain.Entities.TipoMovimiento), m.Tipo.ToString()),
                    Fecha = m.Fecha,
                    Observaciones = m.Observaciones,
                    PedidoId = m.PedidoId
                })
                .ToList();
        }

        // DATOS PARA EL FORM DE DEVOLUCIÓN DEL PANEL: los productos del pedido con
        // lo comprado y lo ya devuelto por producto (devuelta = suma de los
        // movimientos de tipo Devolucion, el único camino que suma stock con
        // traza). Pedido inexistente → null: el controller lo traduce a aviso.
        public async Task<DatosDevolucionDto?> ObtenerDatosDeDevolucionAsync(int pedidoId, CancellationToken ct = default)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .ThenInclude(dp => dp.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, ct);

            if (pedido == null) return null;

            var devueltas = await _context.MovimientosStock
                .AsNoTracking()
                .Where(m => m.Tipo == TipoMovimiento.Devolucion && m.PedidoId == pedidoId)
                .GroupBy(m => m.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(m => m.Cantidad) })
                .ToDictionaryAsync(x => x.ProductoId, x => x.Cantidad, ct);

            return new DatosDevolucionDto
            {
                PedidoId = pedido.PedidoId,
                Estado = pedido.Estado,
                ClienteNombre = pedido.Usuario == null ? null : $"{pedido.Usuario.Nombre} {pedido.Usuario.Apellido}".Trim(),
                AdmiteDevolucion = pedido.Estado is EstadoPedido.Enviado or EstadoPedido.Entregado,
                Items = pedido.DetallesPedido
                    .Where(dp => dp.Producto != null)
                    .Select(dp => new ItemDevolucionDto
                    {
                        ProductoId = dp.ProductoId,
                        ProductoNombre = dp.Producto.Nombre,
                        CantidadComprada = dp.Cantidad,
                        CantidadDevuelta = devueltas.GetValueOrDefault(dp.ProductoId)
                    })
                    .ToList()
            };
        }
    }
}
