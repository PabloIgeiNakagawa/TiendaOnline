using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Usuario;

namespace TiendaOnline.Areas.Admin.ViewModels.Usuario
{
    public class UsuarioListadoViewModel
    {
        public PagedResult<UsuarioListadoDto> UsuariosPaginados { get; set; } = null!;
        public string? Busqueda { get; set; }
        public string? RolSeleccionado { get; set; }
        public bool? EstadoSeleccionado { get; set; }
    }
}
