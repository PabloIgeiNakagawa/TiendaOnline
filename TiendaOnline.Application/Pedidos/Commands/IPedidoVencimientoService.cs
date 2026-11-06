namespace TiendaOnline.Application.Pedidos.Commands
{
    public interface IPedidoVencimientoService
    {
        Task<int> CancelarPedidosVencidosAsync();
    }
}
