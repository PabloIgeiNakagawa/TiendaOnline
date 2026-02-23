using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Admin.Productos
{
    public class EditarProductoViewModel
    {
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        // Propiedad para mostrar la imagen actual en la vista
        public string? ImagenUrlActual { get; set; }

        // Propiedad para subir la nueva imagen (opcional)
        [Display(Name = "Nueva Imagen")]
        public IFormFile? ImagenArchivo { get; set; }
        public IEnumerable<SelectListItem>? Categorias { get; set; }
    }
}
