using System.Net;
using System.Text;
using Moq;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Common;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El webhook de MercadoPago, testeado de punta a punta contra la app real:
    /// ruteo, validator de firma y delegación en ConfirmarPagoAsync.
    /// Los servicios externos van mockeados; la lógica interna de confirmación
    /// ya está cubierta por la suite de servicios (26 tests del flujo de dinero).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class WebhookIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public WebhookIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static StringContent Json(string contenido) => new(contenido, Encoding.UTF8, "application/json");

        [Fact]
        public async Task MercadoPagoWebhook_FirmaInvalida_Retorna401()
        {
            // Arrange: la firma no valida (default del mock) → rechazo sin procesar nada.
            _app.ResetMocks();
            _app.Payment.Setup(x => x.ValidarFirma(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);
            var client = _app.CreateAnonymousClient();

            // Act
            var response = await client.PostAsync("/api/pagos/mercadopago?topic=payment&id=99", Json("{}"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _app.PedidoCommand.Verify(
                x => x.ConfirmarPagoAsync(It.IsAny<InfoPagoDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MercadoPagoWebhook_FirmaValida_ConfirmaElPago()
        {
            // Arrange
            _app.ResetMocks();
            var infoPago = new InfoPagoDto { PedidoId = 1, Estado = "approved", MontoPagado = 100m, TransaccionId = "MP-1" };
            _app.Payment.Setup(x => x.ValidarFirma(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            _app.Payment.Setup(x => x.ObtenerDetallesPagoAsync("99", It.IsAny<CancellationToken>()))
                .ReturnsAsync(infoPago);
            _app.PedidoCommand.Setup(x => x.ConfirmarPagoAsync(infoPago, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResultadoPago.Exito());
            var client = _app.CreateAnonymousClient();

            // Act
            var response = await client.PostAsync("/api/pagos/mercadopago?topic=payment&id=99", Json("{}"));

            // Assert: webhook efectivo (200) y la confirmación delegada usando el pago traído de MP.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _app.PedidoCommand.Verify(
                x => x.ConfirmarPagoAsync(infoPago, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task MercadoPagoWebhook_SinId_Retorna200SinConsultarMercadoPago()
        {
            // Arrange: firma válida pero sin id en query ni en body → OK temprano de cortesía.
            _app.ResetMocks();
            _app.Payment.Setup(x => x.ValidarFirma(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            var client = _app.CreateAnonymousClient();

            // Act
            var response = await client.PostAsync("/api/pagos/mercadopago?topic=payment", Json("{}"));

            // Assert: cortesía de MP (el webhook siempre responde 200 si algo llegó sin vínculo),
            // y sin tocar nada: con el Reset del Arrange, "never" ahora es un conteo confiable.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _app.Payment.Verify(x => x.ObtenerDetallesPagoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _app.PedidoCommand.Verify(
                x => x.ConfirmarPagoAsync(It.IsAny<InfoPagoDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
