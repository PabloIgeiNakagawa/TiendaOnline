using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Categorias.Commands;
using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
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

        public async Task AgregarCategoriaAsync(CategoriaDto categoria, CancellationToken ct = default)
        {
            Categoria nuevaCategoria = new Categoria
            {
                Nombre = categoria.Nombre,
                CategoriaPadreId = categoria.CategoriaPadreId,
                Activa = true // Por defecto, la nueva categoría se activa
            };

            _context.Categorias.Add(nuevaCategoria);
            await _context.SaveChangesAsync(ct);
        }

        public async Task EditarCategoriaAsync(int categoriaId, string nombre, int? categoriaPadreId, CancellationToken ct = default)
        {
            var categoria = await _context.Categorias.FindAsync(categoriaId, ct);
            if (categoria == null)
                throw new CategoriaNoValidaException("La categoría indicada no existe.");

            // El guard corre ANTES de mutar: si el nuevo padre generaría un bucle,
            // nada se guarda (ni el nombre) y el admin ve el aviso con el form vivo.
            if (await VerificarSiCausaBucleAsync(categoriaId, categoriaPadreId, ct))
            {
                throw new CategoriaNoValidaException(
                    "No se puede asignar esa categoría superior: generaría una referencia circular en la jerarquía.");
            }

            categoria.Nombre = nombre;
            categoria.CategoriaPadreId = categoriaPadreId;
            await _context.SaveChangesAsync(ct);
        }

        public async Task EliminarCategoriaAsync(int categoriaId, CancellationToken ct = default)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.CategoriaId == categoriaId, ct);

            if (categoria == null)
                throw new CategoriaNoValidaException("La categoría indicada no existe.");

            // La baja física no puede arrastrar hijos ni productos (Restrict en la base).
            if (categoria.Subcategorias.Count != 0 || categoria.Productos.Count != 0)
                throw new CategoriaNoValidaException("No se puede eliminar: la categoría tiene productos o subcategorías.");

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync(ct);
        }

        private async Task<bool> VerificarSiCausaBucleAsync(int categoriaId, int? nuevoPadreId, CancellationToken ct)
        {
            if (nuevoPadreId == null) return false;

            if (categoriaId == nuevoPadreId) return true;

            int? idActual = nuevoPadreId;

            while (idActual != null)
            {
                var padre = await _context.Categorias
                    .AsNoTracking()
                    .Select(c => new { c.CategoriaId, c.CategoriaPadreId })
                    .FirstOrDefaultAsync(c => c.CategoriaId == idActual, ct);

                if (padre == null) break;

                // Si el ancestro propuesto ya cuelga de la categoría que movemos, el
                // movimiento crearía un ciclo (el abuelo quedaría bajo el nieto).
                if (padre.CategoriaPadreId == categoriaId)
                {
                    return true;
                }

                idActual = padre.CategoriaPadreId;
            }

            return false;
        }
    }
}
