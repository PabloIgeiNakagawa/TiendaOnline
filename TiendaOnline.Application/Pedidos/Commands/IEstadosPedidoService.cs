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
        Task PedidoCanceladoAsync(int pedidoId, CancellationToken ct = default);
    }
}
