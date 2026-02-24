using TiendaOnline.Application.Common;
using TiendaOnline.Application.Productos.Commands;

namespace TiendaOnline.Application.Productos.Queries
{
    public interface IProductoQueryService
    {
        Task<ProductoDto?> ObtenerProductoAsync(int id);
        Task<PagedResult<ProductoListaDto>> ObtenerProductosAdminAsync(ObtenerProductosAdminRequest request);
        Task<PagedResult<ProductoDto>> ObtenerProductosCatalogoAsync(ObtenerProductosCatalogoRequest request);
    }
}
