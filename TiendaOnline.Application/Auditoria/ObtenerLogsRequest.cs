namespace TiendaOnline.Application.Auditoria
{
    public class ObtenerLogsRequest
    {
        public string? Busqueda { get; init; }
        public DateTime? FechaDesde { get; init; }
        public DateTime? FechaHasta { get; init; }
        public int Pagina { get; init; } = 1;
        public int TamanoPagina { get; init; } = 10;
    }
}
