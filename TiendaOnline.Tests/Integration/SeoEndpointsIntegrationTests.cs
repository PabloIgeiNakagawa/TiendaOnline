using System.Net;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Los endpoints de SEO servidos por HomeController: robots.txt bloquea las
    /// zonas privadas y declara el sitemap; sitemap.xml lista home + catálogo +
    /// las fichas de SOLO los productos activos.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class SeoEndpointsIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public SeoEndpointsIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Sitemap_ListaSoloLasFichasDeProductosActivos()
        {
            _app.ResetMocks();

            // Único test que golpea /sitemap.xml: la acción cachea 30 min, así que
            // una segunda corrida vería los ids congelados de la primera.
            var (idActivo, idOtroActivo, idInactivo) = _app.WithDb(ctx =>
            {
                var activo = TestDatabase.SeedProducto(ctx);
                var otroActivo = TestDatabase.SeedProducto(ctx);
                var inactivo = TestDatabase.SeedProducto(ctx);
                inactivo.Activo = false;
                ctx.SaveChanges();
                return (activo.ProductoId, otroActivo.ProductoId, inactivo.ProductoId);
            });

            var client = _app.CreateAnonymousClient();
            var respuesta = await client.GetAsync("/sitemap.xml");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.StartsWith("application/xml", respuesta.Content.Headers.ContentType?.ToString());

            var xml = await respuesta.Content.ReadAsStringAsync();
            Assert.Contains("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">", xml);
            Assert.Contains("/Productos</loc>", xml); // el catálogo
            Assert.Contains($"/Productos/Detalles?id={idActivo}", xml);
            Assert.Contains($"/Productos/Detalles?id={idOtroActivo}", xml);
            Assert.DoesNotContain($"/Productos/Detalles?id={idInactivo}", xml); // los desactivados no se indexan
        }

        [Fact]
        public async Task Robots_BloqueaZonasPrivadasYDeclaraSitemap()
        {
            var client = _app.CreateAnonymousClient();
            var respuesta = await client.GetAsync("/robots.txt");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.StartsWith("text/plain", respuesta.Content.Headers.ContentType?.ToString());

            var texto = await respuesta.Content.ReadAsStringAsync();
            Assert.Contains("User-agent: *", texto);
            Assert.Contains("Disallow: /admin", texto);
            Assert.Contains("Disallow: /hangfire", texto);
            Assert.Contains("Disallow: /Accounts", texto);
            Assert.Contains("Disallow: /Carrito", texto);
            Assert.Contains("Disallow: /Checkout", texto);
            Assert.Contains("Disallow: /Pedidos", texto);
            Assert.Contains("Disallow: /Repartidor", texto);
            Assert.Contains("Sitemap: ", texto);
            Assert.Contains("/sitemap.xml", texto);
        }
    }
}
