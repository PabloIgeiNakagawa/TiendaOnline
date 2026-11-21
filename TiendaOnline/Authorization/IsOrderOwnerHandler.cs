using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Queries;

namespace TiendaOnline.Authorization
{
    public class OrderOwnerHandler : AuthorizationHandler<IsOrderOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPedidoQueryService _pedidoQueryService;

        public OrderOwnerHandler(IHttpContextAccessor httpContextAccessor, IPedidoQueryService pedidoQueryService)
        {
            _httpContextAccessor = httpContextAccessor;
            _pedidoQueryService = pedidoQueryService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsOrderOwnerRequirement requirement)
        {
            if (context.User.IsInRole("Administrador"))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var pedido = await PedidoDelRequest.ObtenerAsync(httpContext, _pedidoQueryService);
            if (pedido == null) return;

            var usuarioId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId)) return;

            if (pedido.UsuarioId.ToString() == usuarioId)
            {
                context.Succeed(requirement);
            }
        }
    }
}