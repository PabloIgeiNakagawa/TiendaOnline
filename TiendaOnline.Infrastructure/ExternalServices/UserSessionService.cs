using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Infrastructure.ExternalServices
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetUserId()
        {
            // Busca el Claim de tipo NameIdentifier (donde guardamos el ID al hacer Login)
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null; // Usuario no logueado
        }
    }
}