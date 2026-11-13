using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Accounts
{
    public class ResetPasswordViewModel
    {
        // Firma de DataProtection que llega en la query del enlace del email.
        public string Token { get; set; }

        // Sin regla de complejidad propia: mismas reglas que el registro (Required + MaxLength 100).
        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MaxLength(100)]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "Se necesita confirmar la contraseña.")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; }
    }
}
