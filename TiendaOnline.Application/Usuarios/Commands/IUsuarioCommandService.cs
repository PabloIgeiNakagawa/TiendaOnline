namespace TiendaOnline.Application.Usuarios.Commands
{
    public interface IUsuarioCommandService
    {
        Task DarAltaUsuarioAsync(int usuarioId, CancellationToken ct = default);
        Task DarBajaUsuarioAsync(int usuarioId, CancellationToken ct = default);
        Task EditarUsuarioAsync(UsuarioUpdateDto dto, CancellationToken ct = default);
    }
}
