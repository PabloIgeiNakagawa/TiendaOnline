using System.Security.Claims;
using TiendaOnline.Services.DTOs.Account;

namespace TiendaOnline.Services.IServices
{
    public interface IAuthService
    {
        Task<ClaimsPrincipal?> GenerarPrincipalAsync(string email, string password);
        Task Register(RegisterDto model);
    }
}
