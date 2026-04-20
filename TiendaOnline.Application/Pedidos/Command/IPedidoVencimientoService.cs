namespace TiendaOnline.Application.Pedidos.Command
{
    public interface IPedidoVencimientoService
    {
        Task<int> CancelarPedidosVencidosAsync();
    }
}
