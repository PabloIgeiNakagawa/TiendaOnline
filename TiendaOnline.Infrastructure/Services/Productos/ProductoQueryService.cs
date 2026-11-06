using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Productos
{
    public class ProductoQueryService : IProductoQueryService
    {
        private readonly TiendaContext _context;
        private readonly IOfertaQueryService _ofertaQueryService;

        public ProductoQueryService(TiendaContext context, IOfertaQueryService ofertaQueryService)
        {
            _context = context;
            _ofertaQueryService = ofertaQueryService;
        }

        public async Task<ProductoDto?> ObtenerProductoAsync(int id, CancellationToken ct)
        {
            if (id <= 0)
                throw new ArgumentException("El id del producto debe ser mayor a 0.");

            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .FirstOrDefaultAsync(p => p.ProductoId == id, ct);

            if (producto is null)
                return null;

            // Precio efectivo para la ficha del producto:
            // si hay oferta vigente, el front muestra el tachado + "vigente hasta".
            var oferta = await _ofertaQueryService.ObtenerOfertaVigenteAsync(producto.ProductoId, ct);

            return new ProductoDto
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                PrecioOferta = oferta?.PrecioOferta,
                FechaFinOferta = oferta?.FechaFin,
                Stock = producto.Stock,
                ImagenUrl = producto.ImagenUrl,
                CategoriaId = producto.CategoriaId,
                CategoriaNombre = producto.Categoria.Nombre
            };
        }
        
        public async Task<PagedResult<ProductoListaDto>> ObtenerProductosAdminAsync(ObtenerProductosAdminRequest request, CancellationToken ct)
        {
            if (request.Pagina <= 0)
                throw new ArgumentException("La página debe ser mayor a 0.");

            if (request.TamanoPagina <= 0)
                throw new ArgumentException("La cantidad por página debe ser mayor a 0.");

            var query = _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .AsQueryable();

            // --- Filtros ---
            if (!string.IsNullOrWhiteSpace(request.Busqueda))
            {
                query = query.Where(p =>
                    p.Nombre.Contains(request.Busqueda) ||
                    p.Descripcion.Contains(request.Busqueda));
            }

            if (request.CategoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == request.CategoriaId);
            }

            if (!string.IsNullOrWhiteSpace(request.Estado))
            {
                query = request.Estado == "activo"
                    ? query.Where(p => p.Activo)
                    : query.Where(p => !p.Activo);
            }

            if (!string.IsNullOrWhiteSpace(request.Stock))
            {
                query = request.Stock switch
                {
                    "agotado" => query.Where(p => p.Stock == 0),
                    "bajo" => query.Where(p => p.Stock > 0 && p.Stock <= 5),
                    "disponible" => query.Where(p => p.Stock > 5),
                    _ => query
                };
            }

            int totalElementos = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.ProductoId)
                .Skip((request.Pagina - 1) * request.TamanoPagina)
                .Take(request.TamanoPagina)
                .Select(p => new ProductoListaDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    Stock = p.Stock,
                    ImagenUrl = p.ImagenUrl,
                    Activo = p.Activo,
                    CategoriaNombre = p.Categoria != null
                        ? p.Categoria.Nombre
                        : "Sin categoría",
                    CategoriaId = p.CategoriaId
                })
                .ToListAsync(ct);

            return new PagedResult<ProductoListaDto>(
                items,
                totalElementos,
                request.Pagina,
                request.TamanoPagina);
        }

        public async Task<PagedResult<ProductoDto>> ObtenerProductosCatalogoAsync(ObtenerProductosCatalogoRequest request, CancellationToken ct)
        {
            var query = _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(request.Busqueda))
            {
                query = query.Where(p =>
                    p.Nombre.Contains(request.Busqueda) ||
                    p.Descripcion.Contains(request.Busqueda));
            }

            if (request.CategoriaId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoriaId == request.CategoriaId ||
                    p.Categoria.CategoriaPadreId == request.CategoriaId);
            }

            // El orden "precio asc/desc" y los filtros de precio corren sobre el PRECIO
            // EFECTIVO (mejor oferta vigente ?? lista): la misma regla con la que se
            // cobra y el número que ve el cliente en la tarjeta. La subquery correlacionada
            // traduce igual en SQL Server y en SQLite (los tests corren contra la DB real).
            var ahora = DateTime.Now;

            if (request.PrecioMin.HasValue)
                query = query.Where(p =>
                    (_context.OfertasProducto
                        .Where(o => o.ProductoId == p.ProductoId && o.Activa && o.FechaInicio <= ahora && o.FechaFin >= ahora)
                        .Min(o => (decimal?)o.PrecioOferta) ?? p.Precio)
                    >= request.PrecioMin.Value);

            if (request.PrecioMax.HasValue)
                query = query.Where(p =>
                    (_context.OfertasProducto
                        .Where(o => o.ProductoId == p.ProductoId && o.Activa && o.FechaInicio <= ahora && o.FechaFin >= ahora)
                        .Min(o => (decimal?)o.PrecioOferta) ?? p.Precio)
                    <= request.PrecioMax.Value);

            // Orden
            query = request.Orden switch
            {
                "precio-asc" => query.OrderBy(p =>
                    _context.OfertasProducto
                        .Where(o => o.ProductoId == p.ProductoId && o.Activa && o.FechaInicio <= ahora && o.FechaFin >= ahora)
                        .Min(o => (decimal?)o.PrecioOferta) ?? p.Precio),
                "precio-desc" => query.OrderByDescending(p =>
                    _context.OfertasProducto
                        .Where(o => o.ProductoId == p.ProductoId && o.Activa && o.FechaInicio <= ahora && o.FechaFin >= ahora)
                        .Min(o => (decimal?)o.PrecioOferta) ?? p.Precio),
                _ => query.OrderBy(p => p.Nombre)
            };

            var total = await query.CountAsync(ct);

            var items = await query
                .Skip((request.Pagina - 1) * request.Cantidad)
                .Take(request.Cantidad)
                .Select(p => new ProductoDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Stock = p.Stock,
                    ImagenUrl = p.ImagenUrl,
                    CategoriaNombre = p.Categoria.Nombre,
                    CategoriaId = p.CategoriaId
                })
                .ToListAsync(ct);

            // Precio efectivo por tarjeta: una sola query para todo el lote.
            var ofertas = await _ofertaQueryService.ObtenerOfertasVigentesAsync(items.Select(i => i.ProductoId), ct);
            foreach (var item in items)
            {
                if (ofertas.TryGetValue(item.ProductoId, out var oferta))
                {
                    item.PrecioOferta = oferta.PrecioOferta;
                    item.FechaFinOferta = oferta.FechaFin;
                }
            }

            return new PagedResult<ProductoDto>(
                items,
                total,
                request.Pagina,
                request.Cantidad
            );
        }
    }
}
