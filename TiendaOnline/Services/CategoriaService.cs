using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly TiendaContext _context;

        public CategoriaService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<Categoria?> ObtenerCategoriaAsync(int id)
        {
            return await _context.Categorias
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
            return categoria.CategoriaPadreId != null && !categoria.Subcategorias.Any();
        }

        public async Task AgregarCategoriaAsync(Categoria categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task EditarCategoriaAsync(int categoriaId, string nombre)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId);
            if (categoria != null)
            {
                categoria.Nombre = nombre;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CambiarCategoriaPadre(int categoriaId, int? nuevaCategoriaPadreId)
        {
            // Validación de seguridad básica
            if (await VerificarSiCausaBucleAsync(categoriaId, nuevaCategoriaPadreId))
            {
                throw new InvalidOperationException("No se puede asignar este padre porque generaría una referencia circular (bucle infinito).");
            }

            var categoria = await _context.Categorias.FindAsync(categoriaId);
            if (categoria != null)
            {
                categoria.CategoriaPadreId = nuevaCategoriaPadreId;
                await _context.SaveChangesAsync();
            }
        }

        // 7. Activar
        public async Task ActivarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId);
            if (categoria != null)
            {
                categoria.Activa = true;
                await _context.SaveChangesAsync();
            }
        }

        // 8. Desactivar
        public async Task DesactivarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId);
            if (categoria != null)
            {
                categoria.Activa = false;
                await _context.SaveChangesAsync();
            }
        }

        // 9. Eliminar (Solo si está vacía)
        public async Task<bool> EliminarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.CategoriaId == categoriaId);

            if (categoria == null) return false;

            // Si tiene hijos o productos, no permitimos borrar (Restrict)
            if (categoria.Subcategorias.Any() || categoria.Productos.Any())
                return false;

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return true;
        }

        // 10. Evitar duplicados
        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower());
        }

        // --- MÉTODOS PRIVADOS DE APOYO ---

        private async Task<bool> VerificarSiEsDescendiente(int categoriaId, int posiblePadreId)
        {
            var actual = await _context.Categorias.FindAsync(posiblePadreId);

            while (actual != null && actual.CategoriaPadreId != null)
            {
                if (actual.CategoriaPadreId == categoriaId) return true;
                actual = await _context.Categorias.FindAsync(actual.CategoriaPadreId);
            }
            return false;
        }

        public async Task<bool> VerificarSiCausaBucleAsync(int categoriaId, int? nuevoPadreId)
        {
            // 1. Si no tiene padre, no hay bucle posible.
            if (nuevoPadreId == null) return false;

            // 2. No puede ser hija de sí misma.
            if (categoriaId == nuevoPadreId) return true;

            // 3. Empezamos a subir desde el nuevo padre propuesto hacia arriba.
            int? idActual = nuevoPadreId;

            while (idActual != null)
            {
                // Buscamos el registro del padre actual.
                var padre = await _context.Categorias
                    .AsNoTracking() // Importante para rendimiento
                    .Select(c => new { c.CategoriaId, c.CategoriaPadreId })
                    .FirstOrDefaultAsync(c => c.CategoriaId == idActual);

                if (padre == null) break;

                // Si en el camino hacia la raíz encontramos el ID de la categoría que estamos moviendo...
                // ¡Hay un bucle! (Porque estaríamos poniendo al abuelo como hijo del nieto).
                if (padre.CategoriaPadreId == categoriaId)
                {
                    return true;
                }

                // Seguimos subiendo.
                idActual = padre.CategoriaPadreId;
            }

            return false;
        }
    }
}
