using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Usuarios
{
    public class CambiarPasswordViewModel
    {
        // Sin regla de complejidad propia: mismas reglas que el registro y el reset
        // (Required + MaxLength 100).
        [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
        [DataType(DataType.Password)]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string ContrasenaNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Se necesita confirmar la contraseña.")]
        [Compare("ContrasenaNueva", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
