using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Areas.Admin.ViewModels.Producto
{
    public class AgregarProductoViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [MaxLength(150)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una categoría")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "La imagen es obligatoria")]
        public IFormFile ImagenArchivo { get; set; }
    }
}
