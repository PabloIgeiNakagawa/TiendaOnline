using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public class Auditoria
    {
        [Key]
        public int AuditoriaId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }

        [Required]
        public string Accion { get; set; }

        [Required]
        public string DatosAnteriores { get; set; }

        [Required]
        public string DatosNuevos { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
