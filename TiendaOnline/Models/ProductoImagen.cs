using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class ProductoImagen
    {
        [Key]
        public int ProductoImagenId { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        [Required, MaxLength(255)]
        public string Url { get; set; }

        [Required]
        public bool EsPrincipal { get; set; }
    }
}
