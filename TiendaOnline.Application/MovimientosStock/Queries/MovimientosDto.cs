namespace TiendaOnline.Application.MovimientosStock.Queries
{
    public class MovimientosDto
    {
        public int MovimientoId { get; set; }
        public string ProductoNombre { get; set; }
        public string ImagenUrl { get; set; }
        public int Cantidad { get; set; }
        public int TipoMovimientoId { get; set; }
        public DateTime Fecha { get; set; }
        public int? PedidoId { get; set; }
        public string Observaciones { get; set; }
    }
}
