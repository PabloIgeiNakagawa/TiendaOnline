using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class EstadoPedido
    {
        [Key]
        public int EstadoPedidoId { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
