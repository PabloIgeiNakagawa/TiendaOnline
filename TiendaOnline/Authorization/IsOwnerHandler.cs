using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TiendaOnline.Authorization
{
    public class IsOwnerHandler : AuthorizationHandler<IsOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IsOwnerHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsOwnerRequirement requirement)
        {
            if (context.User.IsInRole("Administrador"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Obtener el ID de la URL (ej: /Usuarios/Edit/3)
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var resourceId = routeData?.Values["id"]?.ToString();

            // Obtener el ID del usuario autenticado desde los Claims
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (resourceId != null && userId != null && resourceId == userId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
