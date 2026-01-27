using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Services.IServices
{
    public interface IUsuarioService
    {
        Task CrearUsuarioAsync(Usuario usuario);
        Task DarAltaUsuarioAsync(int usuarioId);
        Task DarBajaUsuarioAsync(int usuarioId);
        Task EditarUsuarioAsync(int id, Usuario usuarioEditado);
        Task<Usuario?> ObtenerPorEmailAsync(string email);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
    }
}