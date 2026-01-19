using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class PedidoDetalle
    {
        [Key]
        public int PedidoDetalleId { get; set; }

        [Required]
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public decimal PrecioUnitario { get; set; }
    }
}
