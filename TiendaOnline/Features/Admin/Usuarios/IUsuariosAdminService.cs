using TiendaOnline.Features.Shared.Models;
using TiendaOnline.Features.Tienda.Usuarios;

namespace TiendaOnline.Features.Admin.Usuarios
{
    public interface IUsuariosAdminService
    {
        Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo);
        Task CrearUsuarioAsync(UsuarioCreateDto usuario);
        Task DarAltaUsuarioAsync(int usuarioId);
        Task DarBajaUsuarioAsync(int usuarioId);
        Task EditarUsuarioAsync(UsuarioUpdateDto dto);
    }
}
