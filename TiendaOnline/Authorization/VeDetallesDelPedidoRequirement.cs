using Microsoft.AspNetCore.Authorization;

namespace TiendaOnline.Authorization
{
    /// <summary>
    /// Lectura de la página de Detalles de un pedido: el dueño entra como siempre
    /// con el pedido en cualquier estado; el Repartidor solo sobre los pedidos
    /// que le toca repartir (Enviado) o de los que ya se ocupó (Entregado).
    /// </summary>
    public class VeDetallesDelPedidoRequirement : IAuthorizationRequirement
    {
    }
}
