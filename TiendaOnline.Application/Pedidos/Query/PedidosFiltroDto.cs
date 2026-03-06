namespace TiendaOnline.Application.Pedidos.Query
{
    public class PedidosFiltroDto
    {
        public string? Busqueda { get; set; }
        public int? EstadoId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Monto { get; set; }
        public int Pagina { get; set; } = 1;
        public int Cantidad { get; set; } = 10;
    }
}