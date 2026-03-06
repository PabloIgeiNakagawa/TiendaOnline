using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.Query;

namespace TiendaOnline.Features.Pedidos.Admin
{
    public class PedidoListadoViewModel
    {
        public PagedResult<PedidoListadoDto> PedidosPaginados { get; set; }

        // Filtros
        public string? Busqueda { get; set; }
        public int? EstadoId { get; set; }
        public string EstadoNombre { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? FiltroMonto { get; set; }
    }
}
