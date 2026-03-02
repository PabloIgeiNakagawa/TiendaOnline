namespace TiendaOnline.Application.Pedidos.Command
{
    public interface IPedidoCommandService
    {
        Task<int> CrearPedidoAsync(int usuarioId);
        Task PedidoEnviadoAsync(int pedidoId);
        Task PedidoEntregadoAsync(int pedidoId);
        Task PedidoCanceladoAsync(int pedidoId);
    }
}
