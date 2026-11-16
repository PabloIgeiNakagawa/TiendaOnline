using TiendaOnline.Application.Categorias.Common;

namespace TiendaOnline.Application.Categorias.Commands
{
    public interface ICategoriaCommandService
    {
        // Escritura
        Task AgregarCategoriaAsync(CategoriaDto categoria, CancellationToken ct = default);

        // El nombre y el padre viajan juntos: una sola edición, un solo guard de
        // jerarquía (un padre que haría bucle rechaza ANTES de guardar el nombre).
        Task EditarCategoriaAsync(int categoriaId, string nombre, int? categoriaPadreId, CancellationToken ct = default);

        // Estado y Borrado
        Task ActivarCategoriaAsync(int categoriaId, CancellationToken ct = default);
        Task DesactivarCategoriaAsync(int categoriaId, CancellationToken ct = default);
        Task<bool> EliminarCategoriaAsync(int categoriaId, CancellationToken ct = default); // Solo si no tiene dependencias
    }
}
