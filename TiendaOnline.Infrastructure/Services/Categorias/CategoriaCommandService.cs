using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Categorias.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Categorias
{
    public class CategoriaCommandService : ICategoriaCommandService
    {
        private readonly TiendaContext _context;

        public CategoriaCommandService(TiendaContext context)
        {
            _context = context;
        }

        public async Task AgregarCategoriaAsync(CategoriaDto categoria)
        {
            Categoria nuevaCategoria = new Categoria
            {
                Nombre = categoria.Nombre,
                CategoriaPadreId = categoria.CategoriaPadreId,
                Activa = true // Por defecto, la nueva categoría se activa
            };

            _context.Categorias.Add(nuevaCategoria);
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

        // Activar
        public async Task ActivarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId);
            if (categoria != null)
            {
                categoria.Activa = true;
                await _context.SaveChangesAsync();
            }
        }

        // Desactivar
        public async Task DesactivarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId);
            if (categoria != null)
            {
                categoria.Activa = false;
                await _context.SaveChangesAsync();
            }
        }

        // Eliminar (Solo si está vacía)
        public async Task<bool> EliminarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.CategoriaId == categoriaId);

            if (categoria == null) return false;

            // Si tiene hijos o productos, no permitimos borrar (Restrict)
            if (categoria.Subcategorias.Count != 0 || categoria.Productos.Count != 0)
                return false;

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> VerificarSiCausaBucleAsync(int categoriaId, int? nuevoPadreId)
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
