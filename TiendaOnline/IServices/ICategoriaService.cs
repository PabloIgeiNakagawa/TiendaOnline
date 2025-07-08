using TiendaOnline.Models;

namespace TiendaOnline.IServices
{
    public interface ICategoriaService
    {
        Task<Categoria?> ObtenerCategoriaAsync(int id);
        Task<List<Categoria>> ObtenerCategoriasAsync();
    }
}