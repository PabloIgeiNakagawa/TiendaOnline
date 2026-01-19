using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Descuento
    {
        [Key]
        public int DescuentoId { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        public decimal? Porcentaje { get; set; }
        public decimal? MontoFijo { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        [Required]
        public bool Activo { get; set; }

        public ICollection<ProductoDescuento> ProductoDescuentos { get; set; } = new List<ProductoDescuento>();
    }
}
