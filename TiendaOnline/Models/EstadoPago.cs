using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class EstadoPago
    {
        [Key]
        public int EstadoPagoId { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
