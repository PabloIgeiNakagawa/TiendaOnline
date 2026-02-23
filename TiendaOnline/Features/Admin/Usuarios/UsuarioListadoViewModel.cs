using TiendaOnline.Application.Common;

namespace TiendaOnline.Features.Admin.Usuarios
{
    public class UsuarioListadoViewModel
    {
        public PagedResult<UsuarioListadoDto> UsuariosPaginados { get; set; } = null!;
        public string? Busqueda { get; set; }
        public string? RolSeleccionado { get; set; }
        public bool? EstadoSeleccionado { get; set; }
    }
}
