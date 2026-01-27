using TiendaOnline.Domain.DTOs;
using TiendaOnline.Domain.Entities;
namespace TiendaOnline.Services.IServices
{
    public interface IPedidoService
    {
        Task<Pedido?> ObtenerPedidoAsync(int id);
        Task<List<Pedido>> ObtenerPedidosDeUsuarioAsync(int id);
        Task<List<Pedido>> ObtenerPedidosAsync();
        Task<List<Pedido>> ObtenerPedidosConDetallesAsync();
        Task<Pedido?> ObtenerPedidoConDetallesAsync(int id);
        Task<int> CrearPedidoAsync(List<ItemCarrito> carrito, int usuarioId);
        Task PedidoEnviadoAsync(int pedidoId);
        Task PedidoEntregadoAsync(int pedidoId);
        Task PedidoCanceladoAsync(int pedidoId);
    }
}
