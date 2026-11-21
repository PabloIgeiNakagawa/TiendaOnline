using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TiendaOnline.Application.Direcciones;

namespace TiendaOnline.Authorization
{
    public class IsOwnerHandler : AuthorizationHandler<IsOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDireccionService _direccionService;

        public IsOwnerHandler(IHttpContextAccessor httpContextAccessor, IDireccionService direccionService)
        {
            _httpContextAccessor = httpContextAccessor;
            _direccionService = direccionService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsOwnerRequirement requirement)
        {
            if (context.User.IsInRole("Administrador"))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var routeData = httpContext?.GetRouteData();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return;

            var resourceId = routeData?.Values["id"]?.ToString()
                ?? routeData?.Values["usuarioId"]?.ToString();

            if (resourceId != null && resourceId == userId)
            {
                context.Succeed(requirement);
                return;
            }

            var direccionIdStr = routeData?.Values["direccionId"]?.ToString();
            if (direccionIdStr != null && int.TryParse(direccionIdStr, out var direccionId))
            {
                // null = la dirección no existe: el requirement no se satisface
                // y la autorización niega (el recurso válido siempre responde).
                var direccion = await _direccionService.ObtenerPorIdAsync(direccionId, httpContext?.RequestAborted ?? default);
                if (direccion != null && direccion.UsuarioId.ToString() == userId)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
