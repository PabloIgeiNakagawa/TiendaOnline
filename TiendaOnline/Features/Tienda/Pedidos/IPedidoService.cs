using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.Pedidos;
using TiendaOnline.Features.Shared.Models;
using TiendaOnline.Features.Tienda.Carritos;

namespace TiendaOnline.Features.Tienda.Pedidos
{
    public interface IPedidoService
    {
        Task<Pedido?> ObtenerPedidoAsync(int id);
        Task<List<Pedido>> ObtenerPedidosDeUsuarioAsync(int id);
        Task<List<Pedido>> ObtenerPedidosAsync();
        Task<List<Pedido>> ObtenerPedidosConDetallesAsync();
        Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(string? busqueda, EstadoPedido? estado, DateTime? desde, DateTime? hasta, string? monto, int pagina, int cantidad);
        Task<Pedido?> ObtenerPedidoConDetallesAsync(int id);
        Task<int> CrearPedidoAsync(List<ItemCarrito> carrito, int usuarioId);
        Task PedidoEnviadoAsync(int pedidoId);
        Task PedidoEntregadoAsync(int pedidoId);
        Task PedidoCanceladoAsync(int pedidoId);
    }
}
