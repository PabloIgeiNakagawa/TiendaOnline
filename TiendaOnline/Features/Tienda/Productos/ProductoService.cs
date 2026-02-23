using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Shared.Models;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Features.Tienda.Productos
{
    public class ProductoService : IProductoService
    {
        private readonly TiendaContext _context;

        public ProductoService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<Producto?> ObtenerProductoAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.ProductoId == id);
        }

        public async Task<List<Producto>> ObtenerProductosAsync()
        {
            return await _context.Productos
                                 .AsNoTracking()
                                 .Include(p => p.Categoria)
            .ToListAsync();
        }

        public async Task<PagedResult<ProductoDto>> ObtenerProductosTiendaPaginadoAsync(string busqueda, int? categoriaId, decimal? min, decimal? max, string orden, int pagina, int cantidad)
        {
            var query = _context.Productos
                .AsNoTracking()
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion.Contains(busqueda));

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId || p.Categoria.CategoriaPadreId == categoriaId);

            if (min.HasValue) query = query.Where(p => p.Precio >= min.Value);
            if (max.HasValue) query = query.Where(p => p.Precio <= max.Value);

            // Orden
            query = orden switch
            {
                "precio-asc" => query.OrderBy(p => p.Precio),
                "precio-desc" => query.OrderByDescending(p => p.Precio),
                _ => query.OrderBy(p => p.Nombre)
            };

            int total = await query.CountAsync();

            // Mapeo a DTO y Paginación
            var items = await query
                .Skip((pagina - 1) * cantidad)
                .Take(cantidad)
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

            return new PagedResult<ProductoDto>(items, total, pagina, cantidad);
        }
    }
}
