using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.Query;

namespace TiendaOnline.Features.Pedidos.Admin
{
    public class PedidoListadoViewModel
    {
        public required PagedResult<PedidoListadoDto> PedidosPaginados { get; set; }

        // Filtros
        public string? Busqueda { get; set; }
        public int? EstadoId { get; set; }
        public int? EstadoPagoId { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public decimal? MontoMin { get; set; }
        public decimal? MontoMax { get; set; }

        // Listas para los combos (Selects)
        public required IEnumerable<SelectListItem> EstadosPedido { get; set; }
        public required IEnumerable<SelectListItem> EstadosPago { get; set; }
    }
}
