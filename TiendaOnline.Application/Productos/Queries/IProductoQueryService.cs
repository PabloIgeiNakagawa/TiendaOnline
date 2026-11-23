using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Productos.Queries
{
    public interface IProductoQueryService
    {
        Task<ProductoDto?> ObtenerProductoAsync(int id, CancellationToken ct = default);
        Task<PagedResult<ProductoListaDto>> ObtenerProductosAdminAsync(ObtenerProductosAdminRequest request, CancellationToken ct = default);
        Task<PagedResult<ProductoDto>> ObtenerProductosCatalogoAsync(ObtenerProductosCatalogoRequest request, CancellationToken ct = default);

        /// <summary>Ids de los productos activos (insumo del sitemap). Liviana: solo la PK.</summary>
        Task<List<int>> ObtenerIdsProductosActivosAsync(CancellationToken ct = default);

        /// <summary>
        /// Stock y precio de lista por producto en una sola query (validación del
        /// carrito en lote). Los ids inactivos o inexistentes no tienen entrada.
        /// </summary>
        Task<Dictionary<int, ProductoStockDto>> ObtenerStockPorProductosAsync(IEnumerable<int> productoIds, CancellationToken ct = default);
    }

    /// <summary>Datos mínimos para validar el carrito sin traer la ficha completa.</summary>
    public record ProductoStockDto(int ProductoId, decimal Precio, int Stock);
}
