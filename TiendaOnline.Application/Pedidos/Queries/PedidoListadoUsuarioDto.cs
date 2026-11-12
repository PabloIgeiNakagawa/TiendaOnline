namespace TiendaOnline.Application.Pedidos.Queries
{
    public class PedidoListadoUsuarioDto
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }
        public int EstadoId { get; set; }
        public int EstadoPagoId { get; set; }
        public int MetodoPagoId { get; set; }
        public string? ComprobanteUrl { get; set; }
        public decimal Total { get; set; }
        public int CantidadItems { get; set; }
        public List<PedidoProductoListadoDto> Productos { get; set; } = new();
    }
}
