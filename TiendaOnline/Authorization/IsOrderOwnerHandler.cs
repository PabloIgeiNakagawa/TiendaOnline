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

            // Obtener el ID del Pedido desde la URL (ej: /Pedidos/Detalles/45)
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var orderIdStr = routeData?.Values["id"]?.ToString();

            if (string.IsNullOrEmpty(orderIdStr) || !int.TryParse(orderIdStr, out int orderId))
            {
                return; // No hay un ID válido en la ruta, no podemos validar propiedad
            }

            // Obtener el ID del Usuario logueado desde sus Claims
            var userIdLogueado = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdLogueado))
            {
                return; // Usuario no autenticado
            }

            // Consultar a la Capa de Aplicación (Application)
            // Usamos el servicio que ya tenés definido en tu estructura
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(orderId);

            // Validar: ¿El pedido existe y el dueño es quien dice ser?
            // Importante: Chequeá si en tu DTO el campo es 'UsuarioId' o 'IdUsuario'
            if (pedido != null && pedido.UsuarioId.ToString() == userIdLogueado)
            {
                context.Succeed(requirement);
            }

            // Si no coincide, el sistema por defecto denegará el acceso (403 Forbidden)
        }
    }
}