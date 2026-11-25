using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public class Auditoria
    {
        [Key]
        public int AuditoriaId { get; set; }

        public int? UsuarioId { get; set; }

        public Usuario? Usuario { get; set; }

        [Required]
        public string Accion { get; set; }

        [Required]
        public string TablaAfectada { get; set; }

        [Required]
        public string EntidadId { get; set; }

        [Required]
        public string DatosAnteriores { get; set; }

        [Required]
        public string DatosNuevos { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        // Contexto del request que originó el cambio: en jobs de Hangfire queda
        // nulo (no hay request) y Origen marca "Sistema".
        public string? Ip { get; set; }

        public string? UserAgent { get; set; }

        public string? CorrelationId { get; set; }

        public string? Origen { get; set; }
    }
}
