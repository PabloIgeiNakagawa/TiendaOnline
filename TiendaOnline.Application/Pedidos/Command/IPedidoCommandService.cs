using TiendaOnline.Application.Payment;

namespace TiendaOnline.Application.Pedidos.Command
{
    public interface IPedidoCommandService
    {
        Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto);
        Task<bool> ConfirmarPagoAsync(InfoPagoDto infoPago);
        Task<string> GuardarComprobanteAsync(int pedidoId, Stream archivo, string nombreArchivo);
        Task AprobarPagoTransferenciaAsync(int pedidoId);
        Task RechazarComprobanteTransferenciaAsync(int pedidoId);
        Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId);
        Task PedidoEnviadoAsync(int pedidoId);
        Task PedidoEntregadoAsync(int pedidoId);
        Task PedidoCanceladoAsync(int pedidoId);
    }
}
