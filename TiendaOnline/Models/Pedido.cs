using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public enum EstadoPedido
    {
        Pendiente = 0,
        Enviado = 1,
        Entregado = 2,
        Cancelado = 3
    }

    public class Pedido
    {
        [Key]
        public int PedidoId { get; set; }

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        public DateTime? FechaEnvio { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public DateTime? FechaCancelado { get; set; }

        [Required]
        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

        [Required]
        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }

        public ICollection<DetallePedido> DetallesPedido { get; set; }
    }
}
