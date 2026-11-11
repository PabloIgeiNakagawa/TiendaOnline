using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Moq;
using TiendaOnline.Application.Common;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Agregar al carrito sin cambiar de página: el POST de /Carrito/Agregar
    /// responde SIEMPRE JSON con el resultado del service (el mensaje lo decide
    /// el server) y, si agregó, la cantidad real del carrito para el badge.
    /// El form lo intercepta el JS del front; el endpoint no lee headers.
    /// </summary>
    [Collection("Integracion")]
    public class AgregarCarritoIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public AgregarCarritoIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static string ExtraerTokenAntiforgery(string html)
            => Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;

        private async Task<(HttpResponseMessage respuesta, JsonElement datos)> Postear(int productoId)
        {
            var client = _app.CreateAnonymousClient();

            var detalle = await client.GetAsync($"/Productos/Detalles?id={productoId}");
            Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
            var token = ExtraerTokenAntiforgery(await detalle.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["id"] = productoId.ToString()
            };
            var respuesta = await client.PostAsync("/Carrito/Agregar", new FormUrlEncodedContent(form));
            var datos = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
            return (respuesta, datos);
        }

        [Fact]
        public async Task Agregar_ConExito_JsonConCantidadReal()
        {
            _app.ResetMocks();
            var productoId = _app.WithDb(ctx => TestDatabase.SeedProducto(ctx).ProductoId);

            _app.Carrito.Setup(x => x.AgregarAsync(productoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
            _app.Carrito.Setup(x => x.ObtenerCantidadTotalAsync()).ReturnsAsync(3);

            var (respuesta, datos) = await Postear(productoId);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.True(datos.GetProperty("ok").GetBoolean());
            Assert.Equal("Producto agregado al carrito.", datos.GetProperty("mensaje").GetString());
            Assert.Equal(3, datos.GetProperty("cantidadEnCarrito").GetInt32());
        }

        [Fact]
        public async Task Agregar_ConError_MotivoDelServiceYNadaDeCantidad()
        {
            _app.ResetMocks();
            var productoId = _app.WithDb(ctx => TestDatabase.SeedProducto(ctx).ProductoId);

            _app.Carrito.Setup(x => x.AgregarAsync(productoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure("El producto no está disponible."));

            var (respuesta, datos) = await Postear(productoId);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.False(datos.GetProperty("ok").GetBoolean());
            Assert.Equal("El producto no está disponible.", datos.GetProperty("mensaje").GetString());
            Assert.False(datos.TryGetProperty("cantidadEnCarrito", out _));
        }

        [Fact]
        public async Task Agregar_IdInvalido_BadRequest()
        {
            _app.ResetMocks();
            var client = _app.CreateAnonymousClient();

            var detalle = await client.GetAsync("/Productos");
            Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
            var token = ExtraerTokenAntiforgery(await detalle.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["id"] = "0"
            };
            var respuesta = await client.PostAsync("/Carrito/Agregar", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }
    }
}
