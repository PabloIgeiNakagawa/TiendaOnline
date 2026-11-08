using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Authorization
{
    /// <summary>
    /// Dueño del pedido → acceso como siempre. Repartidor → acceso de lectura
    /// solamente con el pedido Enviado o Entregado: el resto del ciclo (script
    /// de pago, cancelación) es del dueño y del panel administrativo.
    /// </summary>
    public class VeDetallesDelPedidoHandler : AuthorizationHandler<VeDetallesDelPedidoRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPedidoQueryService _pedidoQueryService;

        public VeDetallesDelPedidoHandler(IHttpContextAccessor httpContextAccessor, IPedidoQueryService pedidoQueryService)
        {
            _httpContextAccessor = httpContextAccessor;
            _pedidoQueryService = pedidoQueryService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VeDetallesDelPedidoRequirement requirement)
        {
            if (context.User.IsInRole("Administrador"))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // Mismo contrato que OrderOwnerHandler: el id de la RUTA manda. Query
            // y external_reference cubren los retornos de MercadoPago (el id de
            // un form/body no interpreta nada).
            var pedidoIdStr = httpContext.GetRouteData()?.Values["id"]?.ToString();

            if (string.IsNullOrEmpty(pedidoIdStr))
            {
                pedidoIdStr = httpContext.Request.Query["id"].ToString();
            }

            if (string.IsNullOrEmpty(pedidoIdStr))
            {
                pedidoIdStr = httpContext.Request.Query["external_reference"].ToString();
            }

            if (string.IsNullOrEmpty(pedidoIdStr) || !int.TryParse(pedidoIdStr, out int pedidoId))
            {
                return; // Sin id de pedido en la ruta: acceso denegado.
            }

            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(pedidoId, httpContext.RequestAborted);
            if (pedido == null) return;

            var usuarioId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId)) return;

            // Dueño: mismo acceso que siempre, en cualquier estado del pedido.
            if (pedido.UsuarioId.ToString() == usuarioId)
            {
                context.Succeed(requirement);
                return;
            }

            // Repartidor: lectura de los pedidos en reparto o entregados en sus manos.
            var estadosDeReparto = pedido.EstadoId == (int)EstadoPedido.Enviado
                || pedido.EstadoId == (int)EstadoPedido.Entregado;

            if (context.User.IsInRole("Repartidor") && estadosDeReparto)
            {
                context.Succeed(requirement);
            }
        }
    }
}
