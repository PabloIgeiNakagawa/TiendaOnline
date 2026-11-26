using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Application.Pedidos.Commands
{
    /// <summary>
    /// Asignación del repartidor al pedido: el repartidor toma un pedido de la
    /// cola o el admin lo asigna/reasigna desde el panel. La entrega y el
    /// seguimiento en vivo se apoyan en esa asignación.
    /// </summary>
    public interface IAsignacionRepartidorService
    {
        /// <summary>El repartidor logueado toma un pedido Enviado que nadie tomó.</summary>
        Task TomarPedidoAsync(int pedidoId, CancellationToken ct = default);

        /// <summary>El repartidor logueado suelta su propio pedido (sigue Enviado).</summary>
        Task LiberarPedidoAsync(int pedidoId, CancellationToken ct = default);

        /// <summary>El admin asigna o reasigna; null deja el pedido sin repartidor.</summary>
        Task AsignarRepartidorAsync(int pedidoId, int? repartidorId, CancellationToken ct = default);
    }
}
