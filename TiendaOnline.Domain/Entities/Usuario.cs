using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaOnline.Domain.Entities
{
    public enum Rol
    {
        Usuario,
        Administrador,
        Repartidor
    }

    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [Required]
        public Rol Rol { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Apellido { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaNacimiento { get; set; }

        [MaxLength(25)]
        public string? Telefono { get; set; }

        [Required]
        [MaxLength(50)]
        public string Email { get; set; }

        [Required]
        [MaxLength(100)]
        public string Contrasena { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? UltimaFechaAlta { get; set; }

        public DateTime? UltimaFechaBaja { get; set; }

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public virtual ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
    }

}
