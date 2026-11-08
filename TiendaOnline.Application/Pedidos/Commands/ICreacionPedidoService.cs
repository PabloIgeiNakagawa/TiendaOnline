using TiendaOnline.Application.Payment;

namespace TiendaOnline.Application.Pedidos.Commands
{
    /// <summary>
    /// La creación desde el checkout: items consolidados, snapshot de precio
    /// efectivo (oferta vigente ?? lista) y el contrato para la preferencia de pago.
    /// </summary>
    public interface ICreacionPedidoService
    {
        Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto, CancellationToken ct = default);
    }
}
