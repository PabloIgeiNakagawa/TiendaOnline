using TiendaOnline.Application.Common;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Usuarios.Queries
{
    public interface IUsuarioQueryService
    {
        Task<UsuarioPerfilDto> ObtenerPerfil(int usuarioId, CancellationToken ct = default);
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);
        Task<UsuarioUpdateDto> ObtenerUsuarioParaEdicionAsync(int usuarioId, CancellationToken ct = default);
        Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo, CancellationToken ct = default);
    }
}
