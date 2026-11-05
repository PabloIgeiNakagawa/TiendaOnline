using Microsoft.Extensions.Logging.Abstractions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.MovimientosStock;
using TiendaOnline.Infrastructure.Services.Pedidos;

namespace TiendaOnline.Tests.TestSupport
{
    /// <summary>
    /// Factory de los servicios bajo test: misma estructura de colaboradores
    /// que en producción, con logger nulo para no ensuciar el output.
    /// </summary>
    internal static class Sut
    {
        public static PedidoCommandService PedidoCommand(TiendaContext ctx)
            => new(ctx, new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance), NullLogger<PedidoCommandService>.Instance);

        public static PedidoVencimientoService PedidoVencimiento(TiendaContext ctx)
            => new(ctx, NullLogger<PedidoVencimientoService>.Instance);
    }
}
