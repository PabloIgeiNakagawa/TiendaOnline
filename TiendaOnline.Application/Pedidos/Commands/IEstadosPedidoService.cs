namespace TiendaOnline.Application.Pedidos.Commands
{
    /// <summary>
    /// Las transiciones de vida del pedido: Enviado, Entregado y Cancelado
    /// (con devolución de stock cuando el pedido ya estaba cobrado).
    /// </summary>
    public interface IEstadosPedidoService
    {
        Task PedidoEnviadoAsync(int pedidoId, CancellationToken ct = default);
        Task PedidoEntregadoAsync(int pedidoId, CancellationToken ct = default);

        /// <summary>
        /// Devuelve true cuando el pedido estaba cobrado y quedó con
        /// EstadoPago Reembolsado (el dinero entró y se devuelve; el
        /// controlador usa el dato para el mensaje flash).
        /// </summary>
        Task<bool> PedidoCanceladoAsync(int pedidoId, CancellationToken ct = default);
    }
}
