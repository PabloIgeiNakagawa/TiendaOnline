using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Categorias.Queries
{
    public interface ICategoriaQueryService
    {
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync();
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasRaizAsync();
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasHojaAsync();
        Task<bool> EsCategoriaHojaAsync(int id);
        Task<PagedResult<CategoriaListadoDto>> ObtenerCategoriasPaginadasAsync(int pagina, int cantidad, string? buscar, string? nivel);
        Task<bool> ExisteNombreAsync(string nombre); // Para evitar duplicados
    }
}
