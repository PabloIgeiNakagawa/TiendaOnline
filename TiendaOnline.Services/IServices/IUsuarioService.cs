using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Usuario;
using TiendaOnline.Services.DTOs.Usuario;

namespace TiendaOnline.Services.IServices
{
    public interface IUsuarioService
    {
        Task<Usuario?> ObtenerPorEmailAsync(string email);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
        Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo);
        Task CrearUsuarioAsync(UsuarioCreateDto usuario);
        Task DarAltaUsuarioAsync(int usuarioId);
        Task DarBajaUsuarioAsync(int usuarioId);
        Task EditarUsuarioAsync(int id, Usuario usuarioEditado);
    }
}