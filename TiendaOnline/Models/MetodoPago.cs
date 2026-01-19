using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class MetodoPago
    {
        [Key]
        public int MetodoPagoId { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
