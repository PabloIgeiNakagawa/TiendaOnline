using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Tracking del reparto contra la app real: la toma del pedido desde la cola,
    /// la asignación del admin, el mapa del cliente (JSON solo con el pedido en
    /// camino) y el perímetro de la posición.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class TrackingRepartidorIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public TrackingRepartidorIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static string ExtraerTokenAntiforgery(string html)
        {
            var coincidencia = Regex.Match(
                html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

            Assert.True(coincidencia.Success, "La página no renderizó el token de antiforgery.");
            return coincidencia.Groups[1].Value;
        }

        private (int PedidoId, int ClienteId, int RepartidorId, int OtroId) Sembrar(
            string sufijo, EstadoPedido estado, bool asignado)
        {
            return _app.WithDb(ctx =>
            {
                var cliente = TestDatabase.SeedUsuario(ctx, $"track-cliente-{sufijo}@test.local");
                var repartidor = TestDatabase.SeedUsuario(ctx, $"track-rep-{sufijo}@test.local", rol: Rol.Repartidor);
                var otro = TestDatabase.SeedUsuario(ctx, $"track-otro-{sufijo}@test.local", rol: Rol.Repartidor);
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, cliente, producto, 1,
                    estado: estado,
                    repartidorId: asignado ? repartidor.UsuarioId : null);

                return (pedido.PedidoId, cliente.UsuarioId, repartidor.UsuarioId, otro.UsuarioId);
            });
        }

        [Fact]
        public async Task Repartidor_TomaPedidoLibre_LoAsignaEnLaBase()
        {
            _app.ResetMocks();
            var s = Sembrar("tomar", EstadoPedido.Enviado, asignado: false);

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var response = await client.PostAsync($"/Repartidor/Tomar/{s.PedidoId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var asignado = _app.WithDb(ctx =>
                ctx.Pedidos.Find(s.PedidoId)!.RepartidorId);
            Assert.Equal(s.RepartidorId, asignado);
        }

        [Fact]
        public async Task Repartidor_ColaDeOtro_NoOfreceEntregarElPedidoAjeno()
        {
            _app.ResetMocks();
            var s = Sembrar("ajeno", EstadoPedido.Enviado, asignado: true); // lo tiene el primer repartidor

            var client = _app.CreateClientAs(s.OtroId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            var html = await page.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Contains("Asignado a", html);
            Assert.DoesNotContain($"/Repartidor/Entregar/{s.PedidoId}", html);
        }

        [Fact]
        public async Task Admin_AsignaRepartidorEnPreparacion_LoGuarda()
        {
            _app.ResetMocks();
            var s = Sembrar("asignar", EstadoPedido.EnPreparacion, asignado: false);
            var adminId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "track-admin@test.local", rol: Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var page = await client.GetAsync($"/Pedidos/Detalles/{s.PedidoId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["pedidoId"] = s.PedidoId.ToString(),
                ["repartidorId"] = s.RepartidorId.ToString()
            };
            var response = await client.PostAsync("/admin/pedidos/AsignarRepartidor", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.EndsWith($"/Pedidos/Detalles/{s.PedidoId}", response.Headers.Location?.ToString());
            var asignado = _app.WithDb(ctx =>
                ctx.Pedidos.Find(s.PedidoId)!.RepartidorId);
            Assert.Equal(s.RepartidorId, asignado);
        }

        [Fact]
        public async Task Ubicacion_PedidoEnviado_DevuelveLaPosicionAlDuenio()
        {
            _app.ResetMocks();
            var s = Sembrar("mapa", EstadoPedido.Enviado, asignado: true);
            _app.WithDb(ctx =>
            {
                ctx.UbicacionesRepartidor.Add(new UbicacionRepartidor
                {
                    UsuarioId = s.RepartidorId,
                    Latitud = -34.61,
                    Longitud = -58.39,
                    PrecisionMetros = 12
                });
                ctx.SaveChanges();
                return true;
            });

            var client = _app.CreateClientAs(s.ClienteId, new[] { "Usuario" });
            var response = await client.GetAsync($"/Pedidos/Ubicacion/{s.PedidoId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("disponible").GetBoolean());
            Assert.Equal(-34.61, json.RootElement.GetProperty("latitud").GetDouble(), 2);
            Assert.Equal(-58.39, json.RootElement.GetProperty("longitud").GetDouble(), 2);
        }

        [Fact]
        public async Task Ubicacion_PedidoEntregado_NoExponeLaPosicion()
        {
            _app.ResetMocks();
            var s = Sembrar("entregado", EstadoPedido.Entregado, asignado: true);
            _app.WithDb(ctx =>
            {
                ctx.UbicacionesRepartidor.Add(new UbicacionRepartidor
                {
                    UsuarioId = s.RepartidorId,
                    Latitud = -34.61,
                    Longitud = -58.39
                });
                ctx.SaveChanges();
                return true;
            });

            var client = _app.CreateClientAs(s.ClienteId, new[] { "Usuario" });
            var response = await client.GetAsync($"/Pedidos/Ubicacion/{s.PedidoId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("disponible").GetBoolean());
        }

        /// <summary>
        /// El latido del GPS viaja como JSON (no hay form que cargue el campo
        /// oculto): el token tiene que entrar por el header configurado o el
        /// endpoint devuelve 400 en cada posición y el mapa nunca se mueve.
        /// </summary>
        [Fact]
        public async Task Repartidor_LatidoGpsConTokenEnHeader_GuardaLaPosicion()
        {
            _app.ResetMocks();
            var s = Sembrar("latido", EstadoPedido.Enviado, asignado: true);

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var request = new HttpRequestMessage(HttpMethod.Post, "/Repartidor/Ubicacion")
            {
                Content = new StringContent(
                    """{"latitud":-34.61,"longitud":-58.39,"precisionMetros":12}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("RequestVerificationToken", token);
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var ubicacion = _app.WithDb(ctx =>
                ctx.UbicacionesRepartidor.FirstOrDefault(u => u.UsuarioId == s.RepartidorId));
            Assert.NotNull(ubicacion);
            Assert.Equal(-34.61, ubicacion.Latitud, 2);
        }

        [Fact]
        public async Task Repartidor_CoordenadaInvalida_Devuelve400ConMensaje()
        {
            _app.ResetMocks();
            var s = Sembrar("gps-invalido", EstadoPedido.Enviado, asignado: true);

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var request = new HttpRequestMessage(HttpMethod.Post, "/Repartidor/Ubicacion")
            {
                Content = new StringContent(
                    """{"latitud":91,"longitud":-58.39}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("RequestVerificationToken", token);
            var response = await client.SendAsync(request);

            // Fetch con JSON: el guard responde 400 con mensaje, nunca un redirect.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var noGuardo = _app.WithDb(ctx => !ctx.UbicacionesRepartidor.Any(u => u.UsuarioId == s.RepartidorId));
            Assert.True(noGuardo);
        }

        [Fact]
        public async Task Repartidor_LatidoSinToken_RechazaLaPosicion()
        {
            _app.ResetMocks();
            var s = Sembrar("gps-sin-token", EstadoPedido.Enviado, asignado: true);

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var request = new HttpRequestMessage(HttpMethod.Post, "/Repartidor/Ubicacion")
            {
                Content = new StringContent(
                    """{"latitud":-34.61,"longitud":-58.39}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var noGuardo = _app.WithDb(ctx => !ctx.UbicacionesRepartidor.Any(u => u.UsuarioId == s.RepartidorId));
            Assert.True(noGuardo);
        }
    }
}
