using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Categorias.Queries
{
    public interface ICategoriaQueryService
    {
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct = default);
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasRaizAsync(CancellationToken ct = default);
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasHojaAsync(CancellationToken ct = default);
        Task<bool> EsCategoriaHojaAsync(int id, CancellationToken ct = default);
        Task<PagedResult<CategoriaListadoDto>> ObtenerCategoriasPaginadasAsync(int pagina, int cantidad, string? buscar, string? nivel, CancellationToken ct = default);
        Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct = default); // Para evitar duplicados
    }
}
