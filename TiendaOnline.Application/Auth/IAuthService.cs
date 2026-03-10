namespace TiendaOnline.Application.Auth
{
    public interface IAuthService
    {
        Task<UsuarioDto?> ValidarCredencialesAsync(string email, string password);
        Task RegisterAsync(RegisterDto model);
        Task<UsuarioDto?> ObtenerUsuarioPorEmailAsync(string email);
    }
}
