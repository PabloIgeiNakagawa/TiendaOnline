using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    /// <summary>
    /// La reserva de stock por ítem es un UPDATE condicional: el check (suficiente
    /// stock) y el uso van en la misma instrucción. 0 filas = stock insuficiente o
    /// carrera perdida, y el caller aborta la transacción sin haber tocado nada.
    /// La comparten la confirmación de MercadoPago y la aprobación de transferencia:
    /// la instrucción de dinero más delicada del proyecto vive aquí sola.
    /// </summary>
    internal static class ReservaDeStock
    {
        public static async Task<bool> ReservarAsync(TiendaContext context, ILogger logger, Pedido pedido, CancellationToken ct)
        {
            foreach (var detalle in pedido.DetallesPedido)
            {
                var filasAfectadas = await context.Productos
                    .Where(p => p.ProductoId == detalle.ProductoId && p.Stock >= detalle.Cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - detalle.Cantidad), ct);

                if (filasAfectadas == 0)
                {
                    var nombreProducto = detalle.Producto?.Nombre ?? $"ID {detalle.ProductoId}";
                    logger.LogWarning("Stock insuficiente para aprobar el pedido {PedidoId}. Producto: {ProductoId} ({NombreProducto}), Cantidad: {Cantidad}",
                        pedido.PedidoId, detalle.ProductoId, nombreProducto, detalle.Cantidad);
                    return false;
                }
            }

            return true;
        }
    }
}
