using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.Usuarios;
using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Tienda.Usuarios
{
    public interface IUsuarioService
    {
        Task<UsuarioPerfilDto> ObtenerPerfil(int usuarioId);
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
        Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo);
        Task CrearUsuarioAsync(UsuarioCreateDto usuario);
        Task DarAltaUsuarioAsync(int usuarioId);
        Task DarBajaUsuarioAsync(int usuarioId);
        Task EditarUsuarioAsync(UsuarioUpdateDto dto);
    }
}