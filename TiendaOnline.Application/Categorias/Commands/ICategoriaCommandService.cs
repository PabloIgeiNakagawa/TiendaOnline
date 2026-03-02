namespace TiendaOnline.Application.Categorias.Commands
{
    public interface ICategoriaCommandService
    {
        // Escritura
        Task AgregarCategoriaAsync(CategoriaDto categoria);
        Task EditarCategoriaAsync(int categoriaId, string nombre);
        Task CambiarCategoriaPadre(int categoriaId, int? nuevaCategoriaPadreId);

        // Estado y Borrado
        Task ActivarCategoriaAsync(int categoriaId);
        Task DesactivarCategoriaAsync(int categoriaId);
        Task<bool> EliminarCategoriaAsync(int categoriaId); // Solo si no tiene dependencias
    }
}
