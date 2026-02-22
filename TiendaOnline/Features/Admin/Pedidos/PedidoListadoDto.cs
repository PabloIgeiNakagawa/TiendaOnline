using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Features.Admin.Pedidos
{
    public class PedidoListadoDto
    {
        public int PedidoId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string EmailCliente { get; set; } = string.Empty;
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public decimal Total { get; set; }
        public EstadoPedido Estado { get; set; }
    }
}
