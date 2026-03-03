using TiendaOnline.Application.Common;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Pedidos.Query
{
    public interface IPedidoQueryService
    {
        Task<List<PedidoListadoUsuarioDto>> ObtenerPedidosDeUsuarioAsync(int id);
        Task<Pedido?> ObtenerPedidoConDetallesAsync(int id);
        Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(string? busqueda, EstadoPedido? estado, DateTime? desde, DateTime? hasta, string? monto, int pagina, int cantidad);
    }
}
