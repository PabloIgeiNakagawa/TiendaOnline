using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Tienda.Carritos;

namespace TiendaOnline.Features.Tienda.Pedidos
{
    public interface IPedidoService
    {
        Task<Pedido?> ObtenerPedidoAsync(int id);
        Task<List<Pedido>> ObtenerPedidosDeUsuarioAsync(int id);
        Task<List<Pedido>> ObtenerPedidosAsync();
        Task<List<Pedido>> ObtenerPedidosConDetallesAsync();
        Task<Pedido?> ObtenerPedidoConDetallesAsync(int id);
        Task<int> CrearPedidoAsync(List<ItemCarrito> carrito, int usuarioId);
    }
}
