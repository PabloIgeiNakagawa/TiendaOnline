using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Categorias
{
    public class CategoriaQueryService : ICategoriaQueryService
    {
        private readonly TiendaContext _context;

        public CategoriaQueryService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<Categoria?> ObtenerCategoriaAsync(int id)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Include(c => c.CategoriaPadre)
                .FirstOrDefaultAsync(c => c.CategoriaId == id);
        }

        public async Task<List<Categoria>> ObtenerCategoriasAsync()
        {
            return await _context.Categorias.ToListAsync();
        }

        public async Task<IEnumerable<Categoria>> ObtenerCategoriasRaizAsync()
        {
            return await _context.Categorias
                .AsNoTracking()
                .Include(c => c.Subcategorias)
                .Include(c => c.Productos) // Para mostrar el conteo (Count)
                .Where(c => c.CategoriaPadreId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Categoria>> ObtenerCategoriasHojaAsync()
        {
            return await _context.Categorias
                .Include(c => c.CategoriaPadre)
                .Where(c => !c.Subcategorias.Any())
                .OrderBy(c => c.CategoriaPadre.Nombre)
                .ThenBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<List<Categoria>> ObtenerArbolCategoriasAsync()
        {
            return await _context.Categorias
                .Where(c => c.CategoriaPadreId == null) // Solo las principales
                .Include(c => c.Subcategorias)          // Carga el primer nivel de hijos
                .ToListAsync();
        }

        public async Task<bool> EsCategoriaHojaAsync(int id)
        {
            // Buscamos la categoría específica
            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .FirstOrDefaultAsync(c => c.CategoriaId == id);

            if (categoria == null) return false;

            // CONDICIÓN: Tiene que tener un padre Y NO tener hijos
            return categoria.CategoriaPadreId != null && categoria.Subcategorias.Count == 0;
        }

        public async Task<PagedResult<CategoriaListadoDto>> ObtenerCategoriasPaginadasAsync(int pagina, int cantidad, string? buscar, string? nivel)
        {
            var query = _context.Categorias
                .AsNoTracking()
                .Include(c => c.CategoriaPadre)
                .Include(c => c.Productos)
                .Include(c => c.Subcategorias)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.Nombre.Contains(buscar));

            if (nivel == "Principal")
                query = query.Where(c => c.CategoriaPadreId == null);
            else if (nivel == "Subcategoría")
                query = query.Where(c => c.CategoriaPadreId != null);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.CategoriaPadreId)
                .ThenBy(c => c.Nombre)
                .Skip((pagina - 1) * cantidad)
                .Take(cantidad)
                .Select(c => new CategoriaListadoDto
                {
                    CategoriaId = c.CategoriaId,
                    Nombre = c.Nombre,
                    CategoriaPadreId = c.CategoriaPadreId,
                    CategoriaPadreNombre = c.CategoriaPadre != null ? c.CategoriaPadre.Nombre : null,
                    CantidadProductos = c.Productos.Count,
                    CantidadSubcategorias = c.Subcategorias.Count,
                    Activa = true
                })
                .ToListAsync();

            return new PagedResult<CategoriaListadoDto>(items, total, pagina, cantidad);
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower());
        }
    }
}
