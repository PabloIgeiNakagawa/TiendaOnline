using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Pago
    {
        [Key]
        public int PagoId { get; set; }

        [Required]
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }

        [Required]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        [Required]
        public int EstadoPagoId { get; set; }
        public EstadoPago EstadoPago { get; set; }

        [Required]
        public int MetodoPagoId { get; set; }
        public MetodoPago MetodoPago { get; set; }
    }
}
