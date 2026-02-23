using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Productos.Queries
{
    public interface IProductoQueryService
    {
        Task<PagedResult<ProductoDto>> ObtenerProductosCatalogoAsync(ObtenerProductosCatalogoQuery query);
        Task<ProductoDto?> ObtenerProductoAsync(int id);
    }
}
