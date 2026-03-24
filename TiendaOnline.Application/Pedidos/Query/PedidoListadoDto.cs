namespace TiendaOnline.Application.Pedidos.Query
{
    public class PedidoListadoDto
    {
        public int PedidoId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string EmailCliente { get; set; } = string.Empty;
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public decimal Total { get; set; }
        public int EstadoId { get; set; }
        public int EstadoPagoId { get; set; }
    }
}
