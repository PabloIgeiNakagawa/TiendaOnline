using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    /// <summary>
    /// Métodos de pago del checkout. Los valores fijan la semilla de la tabla
    /// MetodoDePago (migration MetodoDePagoYCambiosPedido): el Id que guarda el
    /// pedido es el int de este enum y todos los services y queries comparan
    /// contra él (punto único de la regla).
    /// </summary>
    public enum MetodoPago
    {
        [Display(Name = "Mercado Pago")]
        MercadoPago = 1,

        [Display(Name = "Transferencia bancaria")]
        Transferencia = 2,

        [Display(Name = "Efectivo")]
        Efectivo = 3
    }

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
