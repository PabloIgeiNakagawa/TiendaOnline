using System.Net;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Sugerencias del buscador de la navbar (Productos/Sugerencias): la misma
    /// consulta del catálogo recortada a 6. El endpoint es anónimo (catálogo
    /// público) y devuelve HTML armado por Razor — el JS de la navbar solo lo
    /// pega debajo del input.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class SugerenciasBusquedaIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public SugerenciasBusquedaIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Sugerencias_ConTerminoCoincidente_TraeLosProductosQueMatchean()
        {
            // Arrange: dos productos con "silla" en el nombre y uno que no matchea.
            _app.ResetMocks();
            _app.WithDb(ctx =>
            {
                TestDatabase.SeedProducto(ctx, nombre: "Silla Gamer Pro");
                TestDatabase.SeedProducto(ctx, nombre: "Silla de Oficina");
                TestDatabase.SeedProducto(ctx, nombre: "Monitor Curvo");
                return true;
            });

            var client = _app.CreateAnonymousClient();

            // Act: minúsculas contra nombres capitalizados — la búsqueda
            // insensible a mayúsculas es comportamiento del server.
            var response = await client.GetAsync("/Productos/Sugerencias?busqueda=silla");

            // Assert: 200 con los dos que matchean y sin el que no.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cuerpo = await response.Content.ReadAsStringAsync();
            Assert.Contains("Silla Gamer Pro", cuerpo);
            Assert.Contains("Silla de Oficina", cuerpo);
            Assert.DoesNotContain("Monitor Curvo", cuerpo);
        }

        [Fact]
        public async Task Sugerencias_SinCoincidencia_MuestraElAviso()
        {
            _app.ResetMocks();
            _app.WithDb(ctx =>
            {
                TestDatabase.SeedProducto(ctx, nombre: "Silla Gamer Pro");
                return true;
            });

            var client = _app.CreateAnonymousClient();

            var response = await client.GetAsync("/Productos/Sugerencias?busqueda=zzzz");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cuerpo = await response.Content.ReadAsStringAsync();
            Assert.Contains("No encontramos productos", cuerpo);
        }

        [Fact]
        public async Task Sugerencias_ConTerminoMuyCorto_RespondeVacio()
        {
            // El JS no pide con menos de 2 caracteres; el guard del server
            // sostiene el mismo contrato contra llamadas directas.
            var client = _app.CreateAnonymousClient();

            var response = await client.GetAsync("/Productos/Sugerencias?busqueda=s");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }
}
