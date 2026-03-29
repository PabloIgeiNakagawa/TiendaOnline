using TiendaOnline.Application.Payment;

namespace TiendaOnline.Application.Pedidos.Command
{
    public interface IPedidoCommandService
    {
        Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto);
        Task<bool> ConfirmarPagoAsync(InfoPagoDto infoPago);
        Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId);
        Task PedidoEnviadoAsync(int pedidoId);
        Task PedidoEntregadoAsync(int pedidoId);
        Task PedidoCanceladoAsync(int pedidoId);
    }
}
