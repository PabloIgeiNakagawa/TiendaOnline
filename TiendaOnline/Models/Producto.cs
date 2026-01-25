using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Producto
    {
        [Key]
        public int ProductoId { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(150)]
        public string Descripcion { get; set; }

        [Required]
        [Precision(18, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Precio { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor a 0.")]
        public int Stock { get; set; }

        [Required]
        public string ImagenUrl { get; set; }

        [Required]
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    }
}
