using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Pedido
    {
        [Key]
        public int PedidoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        [Required]
        public int EstadoPedidoId { get; set; }
        public EstadoPedido EstadoPedido { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Required]
        public decimal Total { get; set; }

        public ICollection<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
