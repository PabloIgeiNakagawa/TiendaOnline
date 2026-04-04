using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Accounts
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MinLength(3, ErrorMessage = "El nombre debe poseer más de 3 caracteres.")]
        [MaxLength(50, ErrorMessage = "El nombre debe poseer menos de 50 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MinLength(3, ErrorMessage = "El apellido debe poseer más de 3 caracteres.")]
        [MaxLength(50, ErrorMessage = "El apellido no puede tener más de 50 caracteres")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FechaNacimiento > DateTime.Today.AddYears(-18))
            {
                yield return new ValidationResult(
                    "El usuario debe tener al menos 18 años.",
                    new[] { nameof(FechaNacimiento) }
                );
            }
        }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [Phone]
        [MaxLength(25)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [MaxLength(50)]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [Display(Name = "Contraseña")]
        [MaxLength(100, ErrorMessage = "La contraseña no puede tener más de 50 caracteres")]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "Se necesita confirmar la contraseña.")]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        public int RolId { get; set; }

        public List<SelectListItem>? RolesDisponibles { get; set; }
    }
}
