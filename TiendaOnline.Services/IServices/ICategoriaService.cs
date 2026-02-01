using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Services.IServices
{
    public interface ICategoriaService
    {
        // Lectura
        Task<Categoria?> ObtenerCategoriaAsync(int id);
        Task<List<Categoria>> ObtenerCategoriasAsync(); // Lista plana
        Task<IEnumerable<Categoria>> ObtenerCategoriasRaizAsync();
        Task<IEnumerable<Categoria>> ObtenerCategoriasHojaAsync();
        Task<List<Categoria>> ObtenerArbolCategoriasAsync(); // Lista jerárquica (Raíces + Subcategorías)
        Task<bool> EsCategoriaHojaAsync(int id);

        // Escritura
        Task AgregarCategoriaAsync(Categoria categoria);
        Task EditarCategoriaAsync(int categoriaId, string nombre);
        Task CambiarCategoriaPadre(int categoriaId, int? nuevaCategoriaPadreId);

        // Estado y Borrado
        Task ActivarCategoriaAsync(int categoriaId);
        Task DesactivarCategoriaAsync(int categoriaId);
        Task<bool> EliminarCategoriaAsync(int categoriaId); // Solo si no tiene dependencias

        // Utilidades para el Admin
        Task<bool> ExisteNombreAsync(string nombre); // Para evitar duplicados
        Task<bool> VerificarSiCausaBucleAsync(int categoriaId, int? nuevoPadreId); // Crucial
    }
}