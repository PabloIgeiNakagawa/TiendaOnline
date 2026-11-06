using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Ofertas;
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
        public static PedidoCommandService PedidoCommand(
            TiendaContext ctx,
            Mock<IImagenService>? imagen = null,
            Mock<IAppSettingsService>? settings = null,
            IOfertaQueryService? ofertas = null)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                imagen?.Object ?? Mock.Of<IImagenService>(),
                settings?.Object ?? Mock.Of<IAppSettingsService>(),
                ofertas ?? Mock.Of<IOfertaQueryService>(),
                NullLogger<PedidoCommandService>.Instance);

        public static PedidoVencimientoService PedidoVencimiento(TiendaContext ctx)
            => new(ctx, NullLogger<PedidoVencimientoService>.Instance);
    }
}
