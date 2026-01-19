using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Auditoria
    {
        [Key]
        public int AuditoriaId { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        [Required, MaxLength(100)]
        public string Accion { get; set; }

        [Required, MaxLength(100)]
        public string Entidad { get; set; }

        public int? EntidadId { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
    }
}
