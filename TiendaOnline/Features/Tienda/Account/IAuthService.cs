using System.Security.Claims;

namespace TiendaOnline.Features.Tienda.Account
{
    public interface IAuthService
    {
        Task<ClaimsPrincipal?> GenerarPrincipalAsync(string email, string password);
        Task Register(RegisterDto model);
    }
}
