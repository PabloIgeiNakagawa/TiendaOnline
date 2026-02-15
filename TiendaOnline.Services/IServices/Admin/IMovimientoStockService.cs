using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.MovimientoStock;

namespace TiendaOnline.Services.IServices.Admin
{
    public interface IMovimientoStockService
    {
        Task<PagedResult<MovimientosDto>> ObtenerMovimientosPaginadosAsync(MovimientoFiltrosDto filtros);
        Task<IEnumerable<TipoMovimientoDTO>> ObtenerTiposMovimientoAsync();
        Task RegistrarEntradaAsync(RegistroStockDto dto);
        Task RegistrarAjusteManualAsync(AjusteManualDto dto);
        Task RegistrarDevolucionAsync(DevolucionStockDto dto);
        Task<List<MovimientoStock>> ObtenerHistorialPorProductoAsync(int productoId);
        // Este método que usa PedidoService internamente
        void GenerarMovimiento(Producto producto, int cantidad, TipoMovimiento tipo, int? pedidoId, string observaciones);
    }
}
