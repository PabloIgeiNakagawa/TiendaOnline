using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Accounts
{
    public class OlvideMiPasswordViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [MaxLength(50)]
        public string Email { get; set; }
    }
}
