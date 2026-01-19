using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class UsuarioDetalles
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public string Apellido { get; set; } = null!;

        public string? Telefono { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}
