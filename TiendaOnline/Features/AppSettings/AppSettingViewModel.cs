using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.AppSettings
{
    public class AppSettingViewModel
    {
        [Required(ErrorMessage = "La clave de configuración es obligatoria.")]
        [StringLength(100, ErrorMessage = "La clave no puede superar los 100 caracteres.")]
        public string Key { get; set; } = null!;

        [StringLength(2000, ErrorMessage = "El valor no puede superar los 2000 caracteres.")]
        public string? Value { get; set; }

        public string? Group { get; set; }

        public bool IsSensitive { get; set; }

        public string Description { get; set; } = null!;

        public string Type { get; set; } = "string";

        public DateTime? LastModified { get; set; }
    }
}
