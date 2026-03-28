namespace TiendaOnline.Application.Usuarios.Commands
{
    public interface IUsuarioCommandService
    {
        Task CrearUsuarioAsync(UsuarioCreateDto usuario);
        Task DarAltaUsuarioAsync(int usuarioId);
        Task DarBajaUsuarioAsync(int usuarioId);
        Task EditarUsuarioAsync(UsuarioUpdateDto dto);
    }
}
