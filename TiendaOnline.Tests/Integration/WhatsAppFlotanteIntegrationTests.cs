using System.Net;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El botón flotante de WhatsApp del layout público: aparece con el número del
    /// panel (AppSettings > Tienda:Whatsapp, link wa.me con solo los dígitos) y no
    /// se muestra si no hay valor configurado. En el layout admin nunca va.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class WhatsAppFlotanteIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public WhatsAppFlotanteIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Home_ConNumeroConfigurado_MuestraElBotonConWaMeLimpio()
        {
            _app.ResetMocks();
            // El número del panel puede venir con espacios, guiones y prefijo: el
            // parcial lo limpia a dígitos (igual que el botón del FAQ).
            _app.Settings.Setup(x => x.GetValue("Tienda:Whatsapp")).Returns("+54 9 351 555-1234");

            var client = _app.CreateAnonymousClient();
            var respuesta = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await respuesta.Content.ReadAsStringAsync();
            Assert.Contains("https://wa.me/5493515551234", html);
            Assert.Contains("btn-whatsapp-flotante", html);
        }

        [Fact]
        public async Task Home_SinNumeroConfigurado_NoMuestraElBoton()
        {
            _app.ResetMocks();
            // Sin setup: GetValue devuelve null (el botón no apunta a un número falso).
            _app.Settings.Setup(x => x.GetValue("Tienda:Whatsapp")).Returns((string?)null);

            var client = _app.CreateAnonymousClient();
            var respuesta = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain("wa.me", html);
        }
    }
}
