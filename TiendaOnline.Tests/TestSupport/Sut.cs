using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Auditoria;
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
        {
            if (ofertas is null)
            {
                // El mock pelado devuelve null para el diccionario del batch;
                // el servicio real siempre entrega un dict (vacío = sin ofertas).
                var mock = new Mock<IOfertaQueryService>();
                mock.Setup(x => x.ObtenerOfertasVigentesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Dictionary<int, OfertaVigenteDto>());
                ofertas = mock.Object;
            }

            return new(ctx, ofertas, NullLogger<CreacionPedidoService>.Instance);
        }

        public static ConfirmacionPagoService ConfirmacionPago(TiendaContext ctx)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                NullLogger<ConfirmacionPagoService>.Instance);

        public static GestionTransferenciaService GestionTransferencia(
            TiendaContext ctx,
            Mock<IImagenService>? imagen = null,
            Mock<IAppSettingsService>? settings = null,
            int? userId = null)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                imagen?.Object ?? Mock.Of<IImagenService>(),
                settings?.Object ?? Mock.Of<IAppSettingsService>(),
                AuditoriaWriter(ctx, userId),
                NullLogger<GestionTransferenciaService>.Instance);

        public static EstadosPedidoService EstadosPedido(TiendaContext ctx, int? userId = null)
            => new(ctx,
                new MovimientoStockCommandService(ctx, NullLogger<MovimientoStockCommandService>.Instance),
                AuditoriaWriter(ctx, userId),
                NullLogger<EstadosPedidoService>.Instance);

        public static PedidoVencimientoService PedidoVencimiento(TiendaContext ctx, int? userId = null)
            => new(ctx, AuditoriaWriter(ctx, userId), NullLogger<PedidoVencimientoService>.Instance);

        /// <summary>
        /// Writer de auditoría con la atribución que tendría una request de ese usuario
        /// (null = job/webhook: la fila se marca Sistema).
        /// </summary>
        private static AuditoriaWriter AuditoriaWriter(TiendaContext ctx, int? userId)
        {
            var session = new Mock<IUserSessionService>();
            session.Setup(x => x.GetUserId()).Returns(userId);
            return new AuditoriaWriter(ctx, session.Object);
        }

        public static Infrastructure.Services.Envios.EnvioQueryService EnvioQuery(TiendaContext ctx)
            => new(ctx, NullLogger<Infrastructure.Services.Envios.EnvioQueryService>.Instance);

        public static Infrastructure.Services.Envios.EnvioCommandService EnvioCommand(TiendaContext ctx)
            => new(ctx, NullLogger<Infrastructure.Services.Envios.EnvioCommandService>.Instance);

        public static Infrastructure.Services.Productos.ProductoCommandService ProductoCommand(
            TiendaContext ctx,
            Mock<IImagenService>? imagen = null)
            => new(ctx, imagen?.Object ?? Mock.Of<IImagenService>());

        public static Infrastructure.Services.Categorias.CategoriaCommandService CategoriaCommand(TiendaContext ctx)
            => new(ctx);
    }
}
