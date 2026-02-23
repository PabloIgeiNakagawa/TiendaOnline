using TiendaOnline.Domain.Entities;
using TiendaOnline.Application.Common;

namespace TiendaOnline.Features.Admin.Pedidos
{
    public interface IPedidosAdminService
    {
        Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(string? busqueda, EstadoPedido? estado, DateTime? desde, DateTime? hasta, string? monto, int pagina, int cantidad);
        Task PedidoEnviadoAsync(int pedidoId);
        Task PedidoEntregadoAsync(int pedidoId);
        Task PedidoCanceladoAsync(int pedidoId);
    }
}
