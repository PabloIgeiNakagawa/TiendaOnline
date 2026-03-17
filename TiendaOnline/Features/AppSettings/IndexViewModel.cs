using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.AppSettings
{
    public class IndexViewModel
    {
        [Required(ErrorMessage = "El grupo de configuración es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre del grupo no puede superar los 100 caracteres.")]
        public string GroupName { get; set; } = null!;

        [Required(ErrorMessage = "Debe existir al menos una configuración.")]
        [MinLength(1, ErrorMessage = "Debe existir al menos una configuración.")]
        public List<AppSettingViewModel> Settings { get; set; } = new();
    }
}
