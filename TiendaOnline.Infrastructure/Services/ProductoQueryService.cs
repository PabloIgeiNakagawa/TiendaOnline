using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services
{
    public class ProductoQueryService : IProductoQueryService
    {
        private readonly TiendaContext _context;

        public ProductoQueryService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<ProductoDto?> ObtenerProductoAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El id del producto debe ser mayor a 0.");

            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto is null)
                return null;

            return new ProductoDto
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock,
                ImagenUrl = producto.ImagenUrl,
                CategoriaId = producto.CategoriaId,
                CategoriaNombre = producto.Categoria.Nombre
            };
        }
        
        public async Task<PagedResult<ProductoListaDto>> ObtenerProductosAdminAsync(ObtenerProductosAdminQuery request)
        {
            if (request.Pagina <= 0)
                throw new ArgumentException("La página debe ser mayor a 0.");

            if (request.RegistrosPorPagina <= 0)
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

            int totalElementos = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.ProductoId)
                .Skip((request.Pagina - 1) * request.RegistrosPorPagina)
                .Take(request.RegistrosPorPagina)
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
                .ToListAsync();

            return new PagedResult<ProductoListaDto>(
                items,
                totalElementos,
                request.Pagina,
                request.RegistrosPorPagina);
        }

        public async Task<PagedResult<ProductoDto>> ObtenerProductosCatalogoAsync(ObtenerProductosCatalogoQuery request)
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

            if (request.PrecioMin.HasValue)
                query = query.Where(p => p.Precio >= request.PrecioMin.Value);

            if (request.PrecioMax.HasValue)
                query = query.Where(p => p.Precio <= request.PrecioMax.Value);

            // Orden
            query = request.Orden switch
            {
                "precio-asc" => query.OrderBy(p => p.Precio),
                "precio-desc" => query.OrderByDescending(p => p.Precio),
                _ => query.OrderBy(p => p.Nombre)
            };

            var total = await query.CountAsync();

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
                .ToListAsync();

            return new PagedResult<ProductoDto>(
                items,
                total,
                request.Pagina,
                request.Cantidad
            );
        }
    }
}
