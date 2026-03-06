namespace TiendaOnline.Features.Pedidos.Admin
{
    public class ListadoFiltrosViewModel
    {
        public string? Busqueda { get; set; }
        public int? EstadoId { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? FiltroMonto { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
}
}