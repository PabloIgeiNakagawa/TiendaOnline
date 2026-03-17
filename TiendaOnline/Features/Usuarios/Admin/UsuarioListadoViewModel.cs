using TiendaOnline.Application.Common;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Features.Usuarios.Admin
{
    public class UsuarioListadoViewModel
    {
        public PagedResult<UsuarioListadoDto> UsuariosPaginados { get; set; } = null!;
        public string? Busqueda { get; set; }
        public string? Rol { get; set; }
        public bool? Estado { get; set; }

        // Listas para los combos (Dropdowns)
        public List<string> RolesDisponibles { get; set; } = new ();
    }
}
