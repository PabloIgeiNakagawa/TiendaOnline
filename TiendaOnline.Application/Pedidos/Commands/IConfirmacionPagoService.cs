using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Common;

namespace TiendaOnline.Application.Pedidos.Commands
{
    /// <summary>
    /// El dinero visto desde MercadoPago: confirmación idempotente del pago
    /// (con claim interno y reserva de stock para el ganador) y los datos
    /// para reintentar un pago fallido.
    /// </summary>
    public interface IConfirmacionPagoService
    {
        Task<ResultadoPago> ConfirmarPagoAsync(InfoPagoDto infoPago, CancellationToken ct = default);
        Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId, CancellationToken ct = default);
    }
}
