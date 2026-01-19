using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Producto
    {
        [Key]
        public int ProductoId { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }

        [Required]
        public decimal Precio { get; set; }

        [Required]
        public bool Activo { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Categoría
        [Required]
        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; }

        // Navegación
        public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
        public ICollection<ProductoDescuento> ProductoDescuentos { get; set; } = new List<ProductoDescuento>();
        public Stock Stock { get; set; }
    }
}
