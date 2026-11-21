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

        // Baja física solo si no tiene dependencias; el resto lanza CategoriaNoValidaException.
        Task EliminarCategoriaAsync(int categoriaId, CancellationToken ct = default);
    }
}
