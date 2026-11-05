using TiendaOnline.Application.Categorias.Common;

namespace TiendaOnline.Application.Categorias.Commands
{
    public interface ICategoriaCommandService
    {
        // Escritura
        Task AgregarCategoriaAsync(CategoriaDto categoria, CancellationToken ct = default);
        Task EditarCategoriaAsync(int categoriaId, string nombre, CancellationToken ct = default);
        Task CambiarCategoriaPadre(int categoriaId, int? nuevaCategoriaPadreId, CancellationToken ct = default);

        // Estado y Borrado
        Task ActivarCategoriaAsync(int categoriaId, CancellationToken ct = default);
        Task DesactivarCategoriaAsync(int categoriaId, CancellationToken ct = default);
        Task<bool> EliminarCategoriaAsync(int categoriaId, CancellationToken ct = default); // Solo si no tiene dependencias
    }
}
