using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.MovimientosStock.Queries
{
    public interface IMovimientoStockQueryService
    {
        Task<PagedResult<MovimientosDto>> ObtenerMovimientosPaginadosAsync(MovimientoFiltrosDto filtros);
        Task<IEnumerable<TipoMovimientoDto>> ObtenerTiposMovimientoAsync();
        Task<List<MovimientosDto>> ObtenerHistorialPorProductoAsync(int productoId);
    }
}
