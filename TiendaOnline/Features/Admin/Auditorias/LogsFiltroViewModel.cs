namespace TiendaOnline.Features.Admin.Auditorias
{
    public class LogsFiltroViewModel
    {
        public string? Busqueda { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
    }
}
