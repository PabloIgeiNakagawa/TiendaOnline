using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public class Direccion
    {
        [Key]
        public int DireccionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Etiqueta { get; set; }

        [Required]
        [MaxLength(100)]
        public string Calle { get; set; }

        [Required]
        [MaxLength(10)]
        public string Numero { get; set; }

        [MaxLength(20)]
        public string? Piso { get; set; }

        [MaxLength(20)]
        public string? Departamento { get; set; }

        [MaxLength(250)]
        public string? Observaciones { get; set; }

        [Required]
        [MaxLength(100)]
        public string Localidad { get; set; }

        [Required]
        [MaxLength(100)]
        public string Provincia { get; set; }

        [Required]
        [MaxLength(15)]
        public string CodigoPostal { get; set; }

        public bool EsPrincipal { get; set; } = false;

        public bool Activo { get; set; } = true;

        // Relación con Usuario
        [Required]
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}
