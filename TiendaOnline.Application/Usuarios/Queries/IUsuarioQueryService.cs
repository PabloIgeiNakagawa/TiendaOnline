using TiendaOnline.Application.Common;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Usuarios.Queries
{
    public interface IUsuarioQueryService
    {
        Task<UsuarioPerfilDto> ObtenerPerfil(int usuarioId);
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
        Task<UsuarioUpdateDto> ObtenerUsuarioParaEdicionAsync(int usuarioId);
        Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo);
    }
}
