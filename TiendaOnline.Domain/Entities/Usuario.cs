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

    public class Usuario : IValidatableObject
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MinLength(3, ErrorMessage = "El nombre debe poseer más de 3 caracteres.")]
        [MaxLength(50, ErrorMessage = "El nombre debe poseer menos de 50 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required]
        public Rol Rol { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MinLength(3, ErrorMessage = "El apellido debe poseer más de 3 caracteres.")]
        [MaxLength(50, ErrorMessage = "El apellido no puede tener más de 50 caracteres")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
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

        [NotMapped]
        [Required(ErrorMessage = "Se necesita confirmar la contraseña.")]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? UltimaFechaAlta { get; set; }

        public DateTime? UltimaFechaBaja { get; set; }

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public virtual ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
    }

}
