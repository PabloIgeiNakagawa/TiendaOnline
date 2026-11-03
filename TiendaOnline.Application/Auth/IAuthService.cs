namespace TiendaOnline.Application.Auth
{
    public interface IAuthService
    {
        Task<UsuarioDto?> ValidarCredencialesAsync(string email, string password);

        /// <summary>
        /// Regla de seguridad #14 (docs/TECH_DEBT.md): el RolId que trae el dto del
        /// cliente NO se respeta por defecto — el rol queda fijado a Usuario.
        /// Solo un caller con privilegios (el controller cuando quien registra es
        /// admin) debe pasar permitirRolPersonalizado: true para elegir el rol.
        /// </summary>
        Task RegisterAsync(RegisterDto model, bool permitirRolPersonalizado = false);

        Task<UsuarioDto?> ObtenerUsuarioPorEmailAsync(string email);
    }
}
