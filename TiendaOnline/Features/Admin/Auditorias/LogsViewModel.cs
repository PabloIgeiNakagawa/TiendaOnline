using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Common;

namespace TiendaOnline.Features.Admin.Auditorias
{
    public class LogsViewModel
    {
        // El resultado que viene del Service
        public PagedResult<AuditoriaListaDto> Paginacion { get; set; } = new PagedResult<AuditoriaListaDto>(new List<AuditoriaListaDto>(), 0, 1, 10);

        // Filtros
        public string? Busqueda { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
