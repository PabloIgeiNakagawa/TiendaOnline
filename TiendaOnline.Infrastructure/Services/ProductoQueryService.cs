using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Domain.Entities;
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
    }
}
