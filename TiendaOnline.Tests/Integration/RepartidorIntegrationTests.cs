using System.Net;
using System.Text.RegularExpressions;
using Moq;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// La cola del reparto de punta a punta contra la app real: listado propio
    /// (pendientes + entregados del último día), la entrega delegando al service
    /// con su guard amable, y el perímetro (usuario común y rutas del panel
    /// administrativo fuera del alcance del rol). Los estados reales del pedido
    /// (transiciones, fechas, stock) los cubren los tests unitarios del service.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class RepartidorIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public RepartidorIntegrationTests(TestApp app)
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

        private static (int PedidoId, int ClienteId, int RepartidorId) SembrarPedido(TestApp app, EstadoPedido estado, string sufijo,
            bool envioADomicilio = false, string? telefono = null, DateTime? fechaEnvio = null, DateTime? fechaEntrega = null)
        {
            return app.WithDb(ctx =>
            {
                var cliente = TestDatabase.SeedUsuario(ctx, $"cliente-reparto-{sufijo}@test.local", telefono: telefono);
                var repartidor = TestDatabase.SeedUsuario(ctx, $"repartidor-{sufijo}@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, cliente, producto, 1, estado: estado,
                    fechaEnvio: fechaEnvio, fechaEntrega: fechaEntrega);

                if (envioADomicilio)
                {
                    // Snapshot de envío cargado en el checkout (el seed por defecto
                    // arma retiro en local).
                    pedido.EsEnvioADomicilio = true;
                    pedido.EnvioCalle = "Calle Falsa";
                    pedido.EnvioNumero = "123";
                    pedido.EnvioLocalidad = "La Plata";
                    pedido.EnvioProvincia = "Buenos Aires";
                    ctx.SaveChanges();
                }

                return (pedido.PedidoId, cliente.UsuarioId, repartidor.UsuarioId);
            });
        }

        [Fact]
        public async Task Repartidor_ListaLosPendientes_DeLaCola()
        {
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "lista");

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");

            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();
            Assert.Contains("Para entregar", html);
            Assert.Contains($"/Repartidor/Entregar/{s.PedidoId}", html);
        }

        [Fact]
        public async Task Repartidor_Entrega_DesdeEnviado_DelegaAlService()
        {
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "entrega");
            _app.EstadosPedido
                .Setup(x => x.PedidoEntregadoAsync(s.PedidoId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var response = await client.PostAsync($"/Repartidor/Entregar/{s.PedidoId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Repartidor", response.Headers.Location?.ToString() ?? string.Empty);
            _app.EstadosPedido.Verify(
                x => x.PedidoEntregadoAsync(s.PedidoId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Repartidor_Entrega_OrdenInvalida_RedirigeConMensajeAmable()
        {
            // El guard del service (solo entregar desde Enviado) sube como
            // DomainException: el filtro global la convierte en mensaje amable con
            // redirect — jamás un 500 bruto.
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.EnPreparacion, "guard");
            _app.EstadosPedido
                .Setup(x => x.PedidoEntregadoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new PedidoNoCancelableException("No se puede entregar: el pedido aún figura como EnPreparacion."));

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var response = await client.PostAsync($"/Repartidor/Entregar/{s.PedidoId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            _app.EstadosPedido.Verify(
                x => x.PedidoEntregadoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Repartidor_PermisosSoloEntrega_PanelAdministrativo_403()
        {
            // El panel administrativo entero queda fuera del alcance del rol (su
            // controller pide Administrador a nivel de clase): el POST de "Entregar"
            // que la auditoría vio permisivo en el action está doblemente bloqueado
            // por ese atributo de clase.
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "perimetro");

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var page = await client.GetAsync("/Repartidor");
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var postEntregar = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["pedidoId"] = s.PedidoId.ToString()
            };
            var responseEntregar = await client.PostAsync("/admin/pedidos/Entregar", new FormUrlEncodedContent(postEntregar));
            var responseEnviar = await client.PostAsync("/admin/pedidos/Enviar", new FormUrlEncodedContent(postEntregar));
            var responseListado = await client.GetAsync("/admin/pedidos/Listado");

            Assert.Equal(HttpStatusCode.Forbidden, responseEntregar.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, responseEnviar.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, responseListado.StatusCode);
        }

        [Fact]
        public async Task Repartidor_UsuarioComun_403()
        {
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "comun");

            // El token+cookie vienen de una página legítima del propio usuario
            // (antiforgery respetado de punta a punta): con ambos en el request,
            // lo que rechaza el 403 es el rol, no el token.
            var client = _app.CreateClientAs(s.ClienteId, new[] { "Usuario" });
            var page = await client.GetAsync($"/Pedidos/Detalles/{s.PedidoId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var getResponse = await client.GetAsync("/Repartidor");
            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var postResponse = await client.PostAsync($"/Repartidor/Entregar/{s.PedidoId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        [Fact]
        public async Task Detalles_Policy_HabilitaRepartidorSoloEnEstadosDeReparto()
        {
            _app.ResetMocks();

            var (pedidoEnviadoId, pedidoNuevoId, clienteId, repartidorId) = _app.WithDb(ctx =>
            {
                var cliente = TestDatabase.SeedUsuario(ctx, "cliente-detalles-reparto@test.local");
                var repartidor = TestDatabase.SeedUsuario(ctx, "repartidor-detalles@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedidoEnviado = TestDatabase.SeedPedido(ctx, cliente, producto, 1, estado: EstadoPedido.Enviado);
                var pedidoNuevo = TestDatabase.SeedPedido(ctx, cliente, producto, 1, estado: EstadoPedido.Nuevo);

                return (pedidoEnviado.PedidoId, pedidoNuevo.PedidoId, cliente.UsuarioId, repartidor.UsuarioId);
            });

            var repartidor = _app.CreateClientAs(repartidorId, new[] { "Repartidor" });
            var dueno = _app.CreateClientAs(clienteId, new[] { "Usuario" });

            var enReparto = await repartidor.GetAsync($"/Pedidos/Detalles/{pedidoEnviadoId}");
            var fueraDeReparto = await repartidor.GetAsync($"/Pedidos/Detalles/{pedidoNuevoId}");

            // El dueño mantiene su acceso completo de siempre (cualquier estado).
            var duenoNuevo = await dueno.GetAsync($"/Pedidos/Detalles/{pedidoNuevoId}");
            var duenoEnviado = await dueno.GetAsync($"/Pedidos/Detalles/{pedidoEnviadoId}");

            Assert.Equal(HttpStatusCode.OK, enReparto.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, fueraDeReparto.StatusCode);
            Assert.Equal(HttpStatusCode.OK, duenoNuevo.StatusCode);
            Assert.Equal(HttpStatusCode.OK, duenoEnviado.StatusCode);
        }

        // El home del reparto es la herramienta de trabajo del rol: dirección
        // siempre visible (en móvil vive bajo el cliente), ruta con el GPS del
        // teléfono, WhatsApp del cliente, items con miniatura y el resumen del día.

        [Fact]
        public async Task Repartidor_EnvioADomicilio_MuestraDireccionRutaYWhatsApp()
        {
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "envio",
                envioADomicilio: true, telefono: "+54 9 11 5555-1234");

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var html = await (await client.GetAsync("/Repartidor")).Content.ReadAsStringAsync();

            // Dirección armada sin ruido (sin "Piso: N/A")...
            Assert.Contains("Calle Falsa 123, La Plata, Buenos Aires", html);
            // ...ruta con GPS del teléfono (destination, sin origin) y chat directo.
            Assert.Contains("maps/dir/?api=1&amp;destination=Calle%20Falsa", html);
            Assert.Contains("wa.me/5491155551234", html);
            // El header de la tabla hereda el tema: sin gris fijo que rompe el modo oscuro.
            Assert.DoesNotContain("table-light", html);
        }

        [Fact]
        public async Task Repartidor_RetiroLocal_MuestraEtiqueta()
        {
            // La columna "Entregar en" nunca queda vacía: el retiro tiene su
            // etiqueta. Que NO ofrezca ruta ni WhatsApp lo cubre el unit test
            // de RepartidorFilaViewModel (acá la cola comparte DB entre tests).
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "retiro");

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var html = await (await client.GetAsync("/Repartidor")).Content.ReadAsStringAsync();

            Assert.Contains("Retiro en local", html);
        }

        [Fact]
        public async Task Repartidor_Items_MuestranMiniaturaNombreYCantidad()
        {
            _app.ResetMocks();
            var s = SembrarPedido(_app, EstadoPedido.Enviado, "items");

            var client = _app.CreateClientAs(s.RepartidorId, new[] { "Repartidor" });
            var html = await (await client.GetAsync("/Repartidor")).Content.ReadAsStringAsync();

            Assert.Contains("chip-producto", html);
            Assert.Contains("https://img.test/producto.png", html);
            Assert.Contains("× 1", html);
        }

        [Fact]
        public async Task Repartidor_ResumenDelDia_CuentaSoloEntregasDeHoy()
        {
            _app.ResetMocks();
            var hoy = SembrarPedido(_app, EstadoPedido.Entregado, "hoy"); // FechaEntrega = ahora
            var ayer = SembrarPedido(_app, EstadoPedido.Entregado, "ayer",
                fechaEntrega: DateTime.Now.AddHours(-30));
            var pendiente = SembrarPedido(_app, EstadoPedido.Enviado, "resumen");

            var client = _app.CreateClientAs(hoy.RepartidorId, new[] { "Repartidor" });
            var html = await (await client.GetAsync("/Repartidor")).Content.ReadAsStringAsync();

            // El resumen cuenta entregas desde la 00:00, no las últimas 24 h
            // (el pendiente de la cola varía con el orden de corrida: DB compartida).
            Assert.Contains("Hoy: 1 entregada · ", html);
        }

        [Fact]
        public async Task Repartidor_EnCaminoHace25Horas_SeResalta()
        {
            _app.ResetMocks();
            var colgado = SembrarPedido(_app, EstadoPedido.Enviado, "colgado",
                fechaEnvio: DateTime.Now.AddHours(-25));
            var fresco = SembrarPedido(_app, EstadoPedido.Enviado, "fresco"); // salió hace instantes

            var client = _app.CreateClientAs(colgado.RepartidorId, new[] { "Repartidor" });
            var html = await (await client.GetAsync("/Repartidor")).Content.ReadAsStringAsync();

            Assert.Contains("en camino hace 25 h", html);
            Assert.Contains("en-camino-colgado", html);
            Assert.Contains("en camino hace instantes", html);
        }
    }
}
