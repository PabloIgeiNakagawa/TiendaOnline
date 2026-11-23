using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Moq;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Guardar la apariencia de punta a punta: si la subida a Cloudinary falla o no
    /// devuelve URL, el logo vigente no se pisa y el admin recibe el aviso. El resto
    /// de los cambios sí se guarda (nombre, colores, fuentes).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class EsteticaIntegrationTests : IClassFixture<TestApp>
    {
        private const string LogoVigente = "https://img.test/logo-vigente.png";
        private const string FaviconVigente = "https://img.test/favicon-vigente.png";

        private readonly TestApp _app;

        public EsteticaIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static string ExtraerTokenAntiforgery(string html)
        {
            var coincidencia = Regex.Match(
                html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

            Assert.True(coincidencia.Success, "El formulario no renderizó el token de antiforgery.");
            return coincidencia.Groups[1].Value;
        }

        private static int SembrarAdmin(TestApp app)
            => app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, $"estetica-{Guid.NewGuid():N}@test.local", Rol.Administrador).UsuarioId);

        private static MultipartFormDataContent Formulario(string token)
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent("Tienda Test"), "NombreDelSitio" },
                { new StringContent(LogoVigente), "LogoUrl" },
                { new StringContent(FaviconVigente), "FavIconUrl" },
                { new StringContent("#0d6efd"), "ColorPrimary" },
                { new StringContent("Inter"), "FuenteTitulo" },
                { new StringContent("Inter"), "FuenteBody" },
                { new StringContent(token), "__RequestVerificationToken" }
            };

            var archivo = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(archivo, "LogoFile", "logo.png");

            return form;
        }

        private void VerificarLogoConservado()
        {
            _app.Settings.Verify(x => x.SaveMultipleSettingsAsync(
                It.Is<IEnumerable<SettingDto>>(dtos => dtos.Any(s =>
                    s.Key == AppSettingsKeys.LogoUrl && s.Value == LogoVigente)),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task<HttpResponseMessage> PostearAsync(HttpClient client, string token)
        {
            using var form = Formulario(token);
            return await client.PostAsync("/admin/app-settings/guardar-estetica", form);
        }

        private async Task<string> GetFormularioAsync(HttpClient client)
        {
            var page = await client.GetAsync("/admin/app-settings/estetica");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            return ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task GuardarEstetica_SubidaLanzaExcepcion_ConservaLogoYAvisa()
        {
            _app.ResetMocks();
            var idAdmin = SembrarAdmin(_app);
            _app.Imagen
                .Setup(x => x.SubirImagenAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Cloudinary caído"));

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var token = await GetFormularioAsync(client);

            var response = await PostearAsync(client, token);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            VerificarLogoConservado();

            var pagina = await client.GetAsync(response.Headers.Location);
            var html = await pagina.Content.ReadAsStringAsync();
            Assert.Contains("No se pudo subir el logo", html);
        }

        [Fact]
        public async Task GuardarEstetica_SubidaSinUrl_ConservaLogoYAvisa()
        {
            _app.ResetMocks();
            var idAdmin = SembrarAdmin(_app);
            _app.Imagen
                .Setup(x => x.SubirImagenAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null!);

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var token = await GetFormularioAsync(client);

            var response = await PostearAsync(client, token);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            VerificarLogoConservado();

            var pagina = await client.GetAsync(response.Headers.Location);
            var html = await pagina.Content.ReadAsStringAsync();
            Assert.Contains("No se pudo subir el logo", html);
        }
    }
}
