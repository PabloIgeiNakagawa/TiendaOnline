namespace TiendaOnline.Application.Usuarios.Commands
{
    public interface IUsuarioCommandService
    {
        Task DarAltaUsuarioAsync(int usuarioId, CancellationToken ct = default);
        Task DarBajaUsuarioAsync(int usuarioId, CancellationToken ct = default);
        // El usuario a editar lo firma el caller en un parámetro propio (el id del
        // route, único valor que la política de ownership valida): el UsuarioId que
        // viaje dentro del dto se ignora siempre — un dato del form no es autorización.
        Task EditarUsuarioAsync(int usuarioId, UsuarioUpdateDto dto, CancellationToken ct = default);
    }
}
