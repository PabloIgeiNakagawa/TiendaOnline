using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Pedidos.Query
{
    public class PedidoListadoUsuarioDto
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }
        public EstadoPedido Estado { get; set; }
        public List<string> Productos { get; set; } = new();
    }
}
