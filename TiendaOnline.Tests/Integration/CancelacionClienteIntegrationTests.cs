using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// La cancelación self-service del cliente (POST /Pedidos/Cancelar/{id}): solo
    /// pedidos Nuevo, nunca con el pago aprobado (el dinero cobrado es caso de
    /// devoluciones), ownership por id de la ruta y antiforgery real. El service
    /// va moqueado: lo que se prueba es el cauce y los guards del controller.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class CancelacionClienteIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public CancelacionClienteIntegrationTests(TestApp app)
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

        [Fact]
        public async Task CancelarPedido_DuenoConPedidoNuevo_DelegaAlServiceYRedirige()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, "cancela-dueno@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, 1, estado: EstadoPedido.Nuevo);
                return (PedidoId: pedido.PedidoId, UsuarioId: usuario.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            // Token+cookie desde MisPedidos: la página del botón Cancelar (que el
            // card renderiza solo para Nuevo — assert incluido).
            var pagina = await client.GetAsync("/Pedidos/MisPedidos");
            Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
            var html = await pagina.Content.ReadAsStringAsync();
            Assert.Contains($"/Pedidos/Cancelar/{ids.PedidoId}", html);
            var token = ExtraerTokenAntiforgery(html);

            var respuesta = await client.PostAsync($"/Pedidos/Cancelar/{ids.PedidoId}", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

            Assert.Equal(HttpStatusCode.Found, respuesta.StatusCode);
            Assert.Contains($"/Pedidos/Detalles/{ids.PedidoId}", respuesta.Headers.Location?.ToString());
            _app.EstadosPedido.Verify(
                x => x.PedidoCanceladoAsync(ids.PedidoId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CancelarPedido_EnPreparacion_NoCancelaConMensajeAmable()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, "cancela-enpreparacion@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var objetivo = TestDatabase.SeedPedido(ctx, usuario, producto, 1, estado: EstadoPedido.EnPreparacion);
                // Donante del token: MisPedidos solo renderiza el form antiforgery
                // cuando hay un card Nuevo (el del pedido cancelable de la lista).
                TestDatabase.SeedPedido(ctx, usuario, producto, 1, estado: EstadoPedido.Nuevo);
                return (PedidoId: objetivo.PedidoId, UsuarioId: usuario.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);
            var pagina = await client.GetAsync("/Pedidos/MisPedidos");
            var html = await pagina.Content.ReadAsStringAsync();
            // El card del pedido en preparación NO ofrece el botón (el server decide).
            Assert.DoesNotContain($"/Pedidos/Cancelar/{ids.PedidoId}", html);
            var token = ExtraerTokenAntiforgery(html);

            var respuesta = await client.PostAsync($"/Pedidos/Cancelar/{ids.PedidoId}", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

            Assert.Equal(HttpStatusCode.Found, respuesta.StatusCode);
            // El guard del controller responde con el aviso de devoluciones (los
            // acentos van como entidades HTML en el source; el fragmento es ASCII).
            var detalles = await client.GetAsync($"/Pedidos/Detalles/{ids.PedidoId}");
            var mensaje = await detalles.Content.ReadAsStringAsync();
            Assert.Contains("en proceso. Para cancelarlo o pedir una devoluci", mensaje);
            _app.EstadosPedido.Verify(
                x => x.PedidoCanceladoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CancelarPedido_PagoAprobado_GuardaDelDineroYNoCancela()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, "cancela-aprobado@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                // Transición rara (pago aprobado con pedido aún Nuevo): el guard del
                // controller es la defensa — cobrado no se cancela desde la cuenta.
                var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, 1,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Aprobado);
                return (PedidoId: pedido.PedidoId, UsuarioId: usuario.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);
            var pagina = await client.GetAsync("/Pedidos/MisPedidos");
            var token = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync());

            var respuesta = await client.PostAsync($"/Pedidos/Cancelar/{ids.PedidoId}", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

            Assert.Equal(HttpStatusCode.Found, respuesta.StatusCode);
            var detalles = await client.GetAsync($"/Pedidos/Detalles/{ids.PedidoId}");
            var html = await detalles.Content.ReadAsStringAsync();
            Assert.Contains("Escribinos y coordinamos la devoluci", html);
            _app.EstadosPedido.Verify(
                x => x.PedidoCanceladoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CancelarPedido_RutaAjena_PoliticaRechazaCon403()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "cancela-victima@test.local");
                var atacante = TestDatabase.SeedUsuario(ctx, "cancela-atacante@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, dueno, producto, 1, estado: EstadoPedido.Nuevo);
                // Pedido del propio atacante: le da la página (con form antiforgery)
                // de la que toma token+cookie, como haría cualquier usuario real.
                TestDatabase.SeedPedido(ctx, atacante, producto, 1, estado: EstadoPedido.Nuevo);
                return (PedidoId: pedido.PedidoId, AtacanteId: atacante.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.AtacanteId);

            // El atacante arma el POST con SU página (como un usuario real).
            var pagina = await client.GetAsync("/Pedidos/MisPedidos");
            var token = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync());

            var respuesta = await client.PostAsync($"/Pedidos/Cancelar/{ids.PedidoId}", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

            // La política (id de la ruta ? usuario) corta antes del action.
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            _app.EstadosPedido.Verify(
                x => x.PedidoCanceladoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);

            var pedido = _app.WithDb(ctx => ctx.Pedidos.AsNoTracking().Single(p => p.PedidoId == ids.PedidoId));
            Assert.Equal(EstadoPedido.Nuevo, pedido.Estado);
        }

        [Fact]
        public async Task CancelarPedido_SinAntiforgery_400()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, "cancela-sintoken@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, 1, estado: EstadoPedido.Nuevo);
                return (PedidoId: pedido.PedidoId, UsuarioId: usuario.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);
            var respuesta = await client.PostAsync($"/Pedidos/Cancelar/{ids.PedidoId}", new FormUrlEncodedContent(
                new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            _app.EstadosPedido.Verify(
                x => x.PedidoCanceladoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
