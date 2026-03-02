using TiendaOnline.Application.Common;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Categorias.Queries
{
    public interface ICategoriaQueryService
    {
        Task<Categoria?> ObtenerCategoriaAsync(int id);
        Task<List<Categoria>> ObtenerCategoriasAsync(); // Lista plana
        Task<IEnumerable<Categoria>> ObtenerCategoriasRaizAsync();
        Task<IEnumerable<Categoria>> ObtenerCategoriasHojaAsync();
        Task<List<Categoria>> ObtenerArbolCategoriasAsync(); // Lista jerárquica (Raíces + Subcategorías)
        Task<bool> EsCategoriaHojaAsync(int id);
        Task<PagedResult<CategoriaListadoDto>> ObtenerCategoriasPaginadasAsync(int pagina, int cantidad, string? buscar, string? nivel);
        Task<bool> ExisteNombreAsync(string nombre); // Para evitar duplicados
    }
}
