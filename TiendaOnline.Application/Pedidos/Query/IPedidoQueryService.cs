using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.DTOs;

namespace TiendaOnline.Application.Pedidos.Query
{
    public interface IPedidoQueryService
    {
        Task<List<PedidoListadoUsuarioDto>> ObtenerPedidosDeUsuarioAsync(int id);
        Task<PedidoDetallesDto?> ObtenerPedidoConDetallesAsync(int id);
        Task<ComprobantePedidoDto?> ObtenerComprobanteDtoAsync(int pedidoId);
        Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(PedidosFiltroDto filtros);
    }
}
