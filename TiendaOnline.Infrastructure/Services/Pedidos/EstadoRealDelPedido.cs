using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    /// <summary>
    /// La re-lectura que sigue a un claim perdido: si el UPDATE condicional
    /// devolvió 0 filas, otro request ya cambió el pedido y el mensaje (o el
    /// motivo del resultado) se arma desde el estado que él dejó en la base,
    /// nunca desde la entidad desactualizada del request perdedor. La comparten
    /// la confirmación de MercadoPago, la gestión de transferencias y la máquina
    /// de estados; el snapshot {Estado, EstadoPago, FechaPedido, RepartidorId}
    /// es todo lo que esos mapeadores necesitan. RepartidorId distingue una
    /// entrega perdida porque el pedido es de otro repartidor.
    /// </summary>
    internal static class EstadoRealDelPedido
    {
        public sealed record Snapshot(EstadoPedido Estado, EstadoPago EstadoPago, DateTime FechaPedido, int? RepartidorId);

        public static async Task<Snapshot?> LeerAsync(TiendaContext context, int pedidoId, CancellationToken ct)
        {
            return await context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new Snapshot(p.Estado, p.EstadoPago, p.FechaPedido, p.RepartidorId))
                .FirstOrDefaultAsync(ct);
        }
    }
}
