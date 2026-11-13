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

        // Cambio de contraseña desde el perfil: verifica la actual contra el hash
        // guardado antes de rehashear la nueva (el id firma el usuario, mismo
        // contrato de ownership que EditarUsuarioAsync).
        Task CambiarPasswordAsync(int usuarioId, string contrasenaActual, string contrasenaNueva, CancellationToken ct = default);
    }
}
