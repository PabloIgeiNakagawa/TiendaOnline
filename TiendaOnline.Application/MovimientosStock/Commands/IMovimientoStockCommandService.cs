using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.MovimientosStock.Commands
{
    public interface IMovimientoStockCommandService
    {
        Task RegistrarEntradaAsync(RegistroStockDto dto);
        Task RegistrarAjusteManualAsync(AjusteManualDto dto);
        Task RegistrarDevolucionAsync(DevolucionStockDto dto);
        void GenerarMovimiento(Producto producto, int cantidad, TipoMovimiento tipo, int? pedidoId, string observaciones);
    }
}
