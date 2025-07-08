using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Login
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Contrasena { get; set; }
    }
}
