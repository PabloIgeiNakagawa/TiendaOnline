using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public class MetodoDePago
    {
        [Key]
        public int MetodoDePagoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } // Ej: "Mercado Pago", "Transferencia Bancaria", "Stripe"

        public bool Activo { get; set; } = true;
    }
}
