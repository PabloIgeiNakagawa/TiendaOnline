using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.Commons.Models;

namespace TiendaOnline.Features.Admin.Pedidos
{
    public class PedidoListadoViewModel
    {
        public PagedResult<PedidoListadoDto> PedidosPaginados { get; set; }

        // Filtros
        public string? Busqueda { get; set; }
        public EstadoPedido? Estado { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? FiltroMonto { get; set; }
    }
}
