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
        Task<List<PedidoListadoDto>> ObtenerPedidosParaExportarAsync(PedidosFiltroDto filtros, CancellationToken ct = default);
        Task<List<PedidoRepartoDto>> ObtenerPedidosEnviadosAsync(CancellationToken ct = default);
        Task<List<PedidoRepartoDto>> ObtenerPedidosEntregadosUltimoDiaAsync(CancellationToken ct = default);

        /// <summary>
        /// Última posición del repartidor asignado, solo mientras el pedido está
        /// Enviado. Null si el pedido no está en camino, no tiene repartidor o
        /// el repartidor todavía no compartió ubicación.
        /// </summary>
        Task<UbicacionRepartidorDto?> ObtenerUbicacionRepartidorAsync(int pedidoId, CancellationToken ct = default);
    }
}
