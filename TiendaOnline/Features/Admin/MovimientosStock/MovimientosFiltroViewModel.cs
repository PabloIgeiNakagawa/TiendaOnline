namespace TiendaOnline.Features.Admin.MovimientosStock
{
    public class MovimientosFiltroViewModel
    {
        public string? Busqueda { get; set; }
        public int? TipoMovimientoId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int RegistrosPorPagina { get; set; } = 20;
    }
}
