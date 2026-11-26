using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    /// <summary>
    /// La última posición del repartidor es un dato efímero de la entrega en
    /// curso: cuando no le quedan pedidos Enviado asignados, la fila sobra.
    /// </summary>
    internal static class UbicacionDeRepartidor
    {
        public static async Task LimpiarSiSinEntregasAsync(TiendaContext context, int repartidorId, CancellationToken ct)
        {
            var tieneEntregasEnCurso = await context.Pedidos
                .AnyAsync(p => p.RepartidorId == repartidorId && p.Estado == EstadoPedido.Enviado, ct);

            if (tieneEntregasEnCurso) return;

            await context.UbicacionesRepartidor
                .Where(u => u.UsuarioId == repartidorId)
                .ExecuteDeleteAsync(ct);
        }
    }
}
