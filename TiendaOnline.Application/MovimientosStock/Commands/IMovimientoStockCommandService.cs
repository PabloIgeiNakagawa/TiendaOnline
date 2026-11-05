using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.MovimientosStock.Commands
{
    public interface IMovimientoStockCommandService
    {
        Task RegistrarEntradaAsync(RegistroStockDto dto, CancellationToken ct = default);
        Task RegistrarAjusteManualAsync(AjusteManualDto dto, CancellationToken ct = default);
        Task RegistrarDevolucionAsync(DevolucionStockDto dto, CancellationToken ct = default);
        void GenerarMovimiento(Producto producto, int cantidad, TipoMovimiento tipo, int? pedidoId, string observaciones);
    }
}
