using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    /// <summary>
    /// Imagen adicional de un producto para la galería del visor de la ficha.
    /// La imagen principal vive en Producto.ImagenUrl (es la que leen catálogo,
    /// carrito, checkout y todo resumen); esta tabla solo guarda las extras,
    /// en el orden de subida.
    /// </summary>
    public class ProductoImagen
    {
        [Key]
        public int ProductoImagenId { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;

        public int Orden { get; set; }
    }
}
