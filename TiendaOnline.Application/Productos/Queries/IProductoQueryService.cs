using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Productos.Queries
{
    public interface IProductoQueryService
    {
        Task<ProductoDto?> ObtenerProductoAsync(int id, CancellationToken ct = default);
        Task<PagedResult<ProductoListaDto>> ObtenerProductosAdminAsync(ObtenerProductosAdminRequest request, CancellationToken ct = default);
        Task<PagedResult<ProductoDto>> ObtenerProductosCatalogoAsync(ObtenerProductosCatalogoRequest request, CancellationToken ct = default);
    }
}
