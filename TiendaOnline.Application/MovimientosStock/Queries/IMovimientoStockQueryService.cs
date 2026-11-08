using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.MovimientosStock.Queries
{
    public interface IMovimientoStockQueryService
    {
        Task<PagedResult<MovimientosDto>> ObtenerMovimientosPaginadosAsync(MovimientoFiltrosDto filtros, CancellationToken ct = default);
        Task<IEnumerable<TipoMovimientoDto>> ObtenerTiposMovimientoAsync(CancellationToken ct = default);
        Task<List<MovimientosDto>> ObtenerHistorialPorProductoAsync(int productoId, CancellationToken ct = default);
        Task<DatosDevolucionDto?> ObtenerDatosDeDevolucionAsync(int pedidoId, CancellationToken ct = default);
    }
}
