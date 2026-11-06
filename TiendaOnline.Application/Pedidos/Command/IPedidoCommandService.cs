using TiendaOnline.Application.Pedidos.Common;
using TiendaOnline.Application.Payment;

namespace TiendaOnline.Application.Pedidos.Command
{
    public interface IPedidoCommandService
    {
        Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto, CancellationToken ct = default);
        Task<ResultadoPago> ConfirmarPagoAsync(InfoPagoDto infoPago, CancellationToken ct = default);
        Task<string> GuardarComprobanteAsync(int pedidoId, Stream archivo, string nombreArchivo, CancellationToken ct = default);
        Task AprobarPagoTransferenciaAsync(int pedidoId, CancellationToken ct = default);
        Task RechazarComprobanteTransferenciaAsync(int pedidoId, CancellationToken ct = default);
        Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId, CancellationToken ct = default);
        Task PedidoEnviadoAsync(int pedidoId, CancellationToken ct = default);
        Task PedidoEntregadoAsync(int pedidoId, CancellationToken ct = default);
        Task PedidoCanceladoAsync(int pedidoId, CancellationToken ct = default);
    }
}
