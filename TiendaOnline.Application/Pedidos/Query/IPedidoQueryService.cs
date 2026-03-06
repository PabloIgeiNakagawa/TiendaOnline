using TiendaOnline.Application.Common;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Pedidos.Query
{
    public interface IPedidoQueryService
    {
        Task<List<PedidoListadoUsuarioDto>> ObtenerPedidosDeUsuarioAsync(int id);
        Task<PedidoDetallesDto?> ObtenerPedidoConDetallesAsync(int id);
        Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(PedidosFiltroDto filtros);
    }
}
