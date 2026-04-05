using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Categorias
{
    public class AgregarCategoriaViewModel
    {
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
        [MaxLength(50, ErrorMessage = "El nombre no puede tener más de 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        public int? CategoriaPadreId { get; set; }
    }
}
