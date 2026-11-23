namespace TiendaOnline.Application.Auditoria
{
    public class ObtenerLogsRequest
    {
        public string? Busqueda { get; init; }
        // Historial de una entidad concreta: los dos campos viajan juntos desde
        // los accesos "Ver historial" del panel.
        public string? TablaAfectada { get; init; }
        public string? EntidadId { get; init; }
        public DateTime? FechaDesde { get; init; }
        public DateTime? FechaHasta { get; init; }
        public int Pagina { get; init; } = 1;
        public int TamanoPagina { get; init; } = 10;
    }
}
