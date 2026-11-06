using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Pedidos.Queries
{
    public interface IPedidoQueryService
    {
        Task<List<PedidoListadoUsuarioDto>> ObtenerPedidosDeUsuarioAsync(int id, CancellationToken ct = default);
        Task<PedidoDetallesDto?> ObtenerPedidoConDetallesAsync(int id, CancellationToken ct = default);
        Task<ComprobantePedidoDto?> ObtenerComprobanteDtoAsync(int pedidoId, CancellationToken ct = default);
        Task<InstruccionesTransferenciaDto?> ObtenerInstruccionesTransferenciaAsync(int pedidoId, CancellationToken ct = default);
        Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(PedidosFiltroDto filtros, CancellationToken ct = default);
    }
}
