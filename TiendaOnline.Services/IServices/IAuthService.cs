using TiendaOnline.Services.DTOs.Account;

namespace TiendaOnline.Services.IServices
{
    public interface IAuthService
    {
        Task<UsuarioDto?> ValidarCredencialesAsync(string email, string password);
    }

}
