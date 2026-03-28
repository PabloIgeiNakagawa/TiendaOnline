using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Query;

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

            // Intentamos obtener el ID del pedido de varios lugares
            // Primero de la ruta (Ej: /Detalles/45)
            var orderIdStr = httpContext.GetRouteData()?.Values["id"]?.ToString();

            // Si no está ahí, buscamos en el Query String (Ej: ?id=45)
            if (string.IsNullOrEmpty(orderIdStr))
            {
                orderIdStr = httpContext.Request.Query["id"].ToString();
            }

            // Si sigue vacío, buscamos el parámetro que envía Mercado Pago (external_reference)
            if (string.IsNullOrEmpty(orderIdStr))
            {
                orderIdStr = httpContext.Request.Query["external_reference"].ToString();
            }

            // Validamos que el ID sea un número válido
            if (string.IsNullOrEmpty(orderIdStr) || !int.TryParse(orderIdStr, out int orderId))
            {
                return; // No se encontró ID, el acceso será denegado
            }

            // Obtener el ID del Usuario logueado (Claim NameIdentifier)
            var userIdLogueado = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdLogueado)) return;

            // Consultar el pedido en la base de datos
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(orderId);

            // Validar propiedad: El pedido existe y el UsuarioId coincide
            if (pedido != null && pedido.UsuarioId.ToString() == userIdLogueado)
            {
                context.Succeed(requirement);
            }

            // Si llegamos acá sin context.Succeed(), ASP.NET devuelve 403 Forbidden automáticamente
        }
    }
}