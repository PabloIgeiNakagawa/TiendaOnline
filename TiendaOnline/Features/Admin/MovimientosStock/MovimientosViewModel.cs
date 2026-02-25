using TiendaOnline.Application.Common;
using TiendaOnline.Application.MovimientosStock.Queries;

namespace TiendaOnline.Features.Admin.MovimientosStock
{
    public class MovimientosViewModel
    {
        public PagedResult<MovimientosDto> MovimientosPaginados { get; set; }

        public string? Busqueda { get; set; }
        public int? TipoMovimientoId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int RegistrosPorPagina { get; set; } = 20;

        // Listas para los Dropdowns de los filtros
        public IEnumerable<TipoMovimientoDto> TiposMovimiento { get; set; } = new List<TipoMovimientoDto>();
    }
}
