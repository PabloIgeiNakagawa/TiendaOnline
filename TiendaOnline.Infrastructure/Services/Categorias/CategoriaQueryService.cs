using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Common;
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

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct = default)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Select(c => new CategoriaDto
                {
                    CategoriaId = c.CategoriaId,
                    Nombre = c.Nombre,
                    CategoriaPadreId = c.CategoriaPadreId
                })
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasRaizAsync(CancellationToken ct = default)
        {
            var categoriasRaiz = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.CategoriaPadreId == null)
                .Select(c => new CategoriaDto
                {
                    CategoriaId = c.CategoriaId,
                    Nombre = c.Nombre,
                    CategoriaPadreId = c.CategoriaPadreId,
                    Subcategorias = c.Subcategorias.Select(s => new CategoriaDto
                    {
                        CategoriaId = s.CategoriaId,
                        Nombre = s.Nombre,
                        CategoriaPadreId = s.CategoriaPadreId
                    }).ToList()
                })
                .ToListAsync(ct);

            return categoriasRaiz;
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasHojaAsync(CancellationToken ct = default)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Where(c => !c.Subcategorias.Any())
                .OrderBy(c => c.CategoriaPadre.Nombre)
                .ThenBy(c => c.Nombre)
                .Select(c => new CategoriaDto
                {
                    CategoriaId = c.CategoriaId,
                    Nombre = c.Nombre,
                    CategoriaPadreId = c.CategoriaPadreId,
                    CategoriaPadre = c.CategoriaPadre != null ? new CategoriaDto
                    {
                        CategoriaId = c.CategoriaPadre.CategoriaId,
                        Nombre = c.CategoriaPadre.Nombre
                    } : null
                })
                .ToListAsync(ct);
        }

        public async Task<bool> EsCategoriaHojaAsync(int id, CancellationToken ct = default)
        {
            // Buscamos la categoría específica
            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .FirstOrDefaultAsync(c => c.CategoriaId == id, ct);

            if (categoria == null) return false;

            // CONDICIÓN: Tiene que tener un padre Y NO tener hijos
            return categoria.CategoriaPadreId != null && categoria.Subcategorias.Count == 0;
        }

        public async Task<PagedResult<CategoriaListadoDto>> ObtenerCategoriasPaginadasAsync(int pagina, int cantidad, string? buscar, string? nivel, CancellationToken ct = default)
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

            var total = await query.CountAsync(ct);

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
                .ToListAsync(ct);

            return new PagedResult<CategoriaListadoDto>(items, total, pagina, cantidad);
        }

        public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct = default)
        {
            return await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower(), ct);
        }
    }
}
