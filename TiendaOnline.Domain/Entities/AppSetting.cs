using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public class AppSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; } = null!; // Ej: "Tienda:WhatsApp"

        [MaxLength(500)]
        public string? Value { get; set; } // Puede ser nulo si el admin lo deja vacío

        [MaxLength(50)]
        public string? Group { get; set; } // Ej: "Pagos", "Contacto", "Diseno"

        public bool IsSensitive { get; set; }

        [MaxLength(200)]
        public string Description { get; set; } = null!; // Descripción para que el admin entienda qué hace esta configuración

        [MaxLength(20)]
        public string Type { get; set; } = "text";

        public DateTime? LastModified { get; set; }

    }
}
