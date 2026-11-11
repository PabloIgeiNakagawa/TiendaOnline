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
        public static CreacionPedidoService CreacionPedido(
            TiendaContext ctx,
            IOfertaQueryService? ofertas = null)
            => new(ctx,
                ofertas ?? Mock.Of<IOfertaQueryService>(),
                NullLogger<CreacionPedidoService>.Instance);

        public static ConfirmacionPagoService ConfirmacionPago(TiendaContext ctx)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                NullLogger<ConfirmacionPagoService>.Instance);

        public static GestionTransferenciaService GestionTransferencia(
            TiendaContext ctx,
            Mock<IImagenService>? imagen = null,
            Mock<IAppSettingsService>? settings = null)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                imagen?.Object ?? Mock.Of<IImagenService>(),
                settings?.Object ?? Mock.Of<IAppSettingsService>(),
                NullLogger<GestionTransferenciaService>.Instance);

        public static EstadosPedidoService EstadosPedido(TiendaContext ctx)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                NullLogger<EstadosPedidoService>.Instance);

        public static PedidoVencimientoService PedidoVencimiento(TiendaContext ctx)
            => new(ctx, NullLogger<PedidoVencimientoService>.Instance);

        public static Infrastructure.Services.Envios.EnvioQueryService EnvioQuery(TiendaContext ctx)
            => new(ctx, NullLogger<Infrastructure.Services.Envios.EnvioQueryService>.Instance);

        public static Infrastructure.Services.Envios.EnvioCommandService EnvioCommand(TiendaContext ctx)
            => new(ctx, NullLogger<Infrastructure.Services.Envios.EnvioCommandService>.Instance);

        public static Infrastructure.Services.Productos.ProductoCommandService ProductoCommand(
            TiendaContext ctx,
            Mock<IImagenService>? imagen = null)
            => new(ctx, imagen?.Object ?? Mock.Of<IImagenService>());
    }
}
