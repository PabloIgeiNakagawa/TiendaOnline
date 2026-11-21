using TiendaOnline.Application.Pedidos.Queries;

namespace TiendaOnline.Authorization
{
    /// <summary>
    /// Resuelve el pedido de la request para las policies de pedidos. El id de la
    /// RUTA manda; el query string y external_reference cubren los retornos de
    /// MercadoPago. Un id de form o body nunca es autorización.
    /// </summary>
    internal static class PedidoDelRequest
    {
        public static async Task<PedidoDetallesDto?> ObtenerAsync(HttpContext httpContext, IPedidoQueryService pedidoQueryService)
        {
            var pedidoIdTexto = httpContext.GetRouteData()?.Values["id"]?.ToString();

            if (string.IsNullOrEmpty(pedidoIdTexto))
                pedidoIdTexto = httpContext.Request.Query["id"].ToString();

            if (string.IsNullOrEmpty(pedidoIdTexto))
                pedidoIdTexto = httpContext.Request.Query["external_reference"].ToString();

            if (!int.TryParse(pedidoIdTexto, out int pedidoId))
                return null;

            return await pedidoQueryService.ObtenerPedidoConDetallesAsync(pedidoId, httpContext.RequestAborted);
        }
    }
}
