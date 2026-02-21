using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.MovimientoStock;

namespace TiendaOnline.Features.Admin.MovimientosStock
{
    public class MovimientosViewModel
    {
        public PagedResult<MovimientosDto> MovimientosPaginados { get; set; }

        public MovimientoFiltrosDto Filtros { get; set; }

        // Listas para los Dropdowns de los filtros
        public IEnumerable<TipoMovimientoDTO> TiposMovimiento { get; set; } = new List<TipoMovimientoDTO>();
    }
}
