using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Tienda.Account
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Contrasena { get; set; }
    }
}
