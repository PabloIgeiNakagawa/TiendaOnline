using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Rol
    {
        [Key]
        public int RolId { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
