using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Regla de ownership del front: el id que decide es el de la RUTA (el único
    /// valor que la política valida), nunca el que viaja en el form. Un POST con
    /// el hidden otrositado al id de la víctima no edita su perfil (IDOR 07/11),
    /// y ReintentarPago con el id en la ruta vuelve a responder — antes moría en
    /// 403 para siempre porque el handler solo miraba el body.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class OwnershipIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public OwnershipIntegrationTests(TestApp app)
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
        public async Task EditarUsuario_ConUsuarioIdAjenoEnElForm_EditaSoloElDeLaRuta()
        {
            // Arrange: dos usuarios reales; el POST va como el dueño pero con el
            // hidden "UsuarioId" otrositado al id de la víctima.
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "perfil-dueno@test.local");
                var victima = TestDatabase.SeedUsuario(ctx, "perfil-victima@test.local");
                return (DuenoId: dueno.UsuarioId, VictimaId: victima.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.DuenoId);
            var page = await client.GetAsync($"/Usuarios/Editar/{ids.DuenoId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["UsuarioId"] = ids.VictimaId.ToString(), // el ataque: id ajeno en el hidden
                ["Nombre"] = "Dueno Editado",
                ["Apellido"] = "Por Ruta",
                ["Telefono"] = "351-000-0000"
            };

            // Act
            var response = await client.PostAsync($"/Usuarios/Editar/{ids.DuenoId}", new FormUrlEncodedContent(form));

            // Assert: solo el usuario del route cambió; la víctima queda intacta.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Usuarios/Perfil", response.Headers.Location?.ToString() ?? string.Empty);

            var (dueno, victima) = _app.WithDb(ctx => (
                ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == ids.DuenoId),
                ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == ids.VictimaId)));
            Assert.Equal("Dueno Editado", dueno.Nombre);
            Assert.Equal("Por Ruta", dueno.Apellido);
            Assert.Equal("Test", victima.Nombre);
            Assert.Equal("User", victima.Apellido);
        }

        [Fact]
        public async Task EditarUsuario_IdAjenoEnLaRuta_PoliticaRechazaCon403()
        {
            // Arrange: el atacante toma token+cookie de SU PROPIA página de edición
            // (como cualquier usuario real) y apunta el POST a la ruta ajena.
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "perfil-victima-ataque@test.local");
                var atacante = TestDatabase.SeedUsuario(ctx, "perfil-atacante@test.local");
                return (DuenoId: dueno.UsuarioId, AtacanteId: atacante.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.AtacanteId);
            var page = await client.GetAsync($"/Usuarios/Editar/{ids.AtacanteId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Nombre"] = "Hackeado",
                ["Apellido"] = "Atacante",
                ["Telefono"] = "351-111-1111"
            };

            // Act: POST directo a la ruta de la víctima
            var response = await client.PostAsync($"/Usuarios/Editar/{ids.DuenoId}", new FormUrlEncodedContent(form));

            // Assert: la política (route id ≠ usuario) corta antes del action.
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var (dueno, atacante) = _app.WithDb(ctx => (
                ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == ids.DuenoId),
                ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == ids.AtacanteId)));
            Assert.Equal("Test", dueno.Nombre);
            Assert.Equal("Test", atacante.Nombre);
        }

        [Fact]
        public async Task ReintentarPago_ConIdEnLaRuta_RedirigeAPreferenciaNueva()
        {
            // Arrange: pedido Pendiente del usuario; los dos pasos (cargar datos y
            // generar preferencia) van moqueados: lo que se prueba es el cauce.
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, "reintento-pago@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, 1, estadoPago: EstadoPago.Pendiente);
                return (PedidoId: pedido.PedidoId, UsuarioId: usuario.UsuarioId);
            });

            _app.PedidoCommand
                .Setup(x => x.ObtenerDatosParaPagoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PedidoPagoDto { PedidoId = ids.PedidoId });
            _app.Payment
                .Setup(x => x.GenerarPreferenciaPagoAsync(It.IsAny<PedidoPagoDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://sandbox.mercadopago.test/preferencia");

            var client = _app.CreateClientAs(ids.UsuarioId);

            // Token+cookie desde PagoFallido: la vista del botón "reintentar".
            var page = await client.GetAsync($"/Pedidos/PagoFallido?external_reference={ids.PedidoId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            // Act: el id viaja SOLO en la ruta — el form ya no lleva hidden (antes
            // eso dejaba al endpoint en 403 eterno).
            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var response = await client.PostAsync($"/Pedidos/ReintentarPago/{ids.PedidoId}", new FormUrlEncodedContent(form));

            // Assert: 302 a la preferencia generada con el pedido cargado una vez.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("https://sandbox.mercadopago.test/preferencia", response.Headers.Location?.ToString());
            _app.PedidoCommand.Verify(
                x => x.ObtenerDatosParaPagoAsync(ids.PedidoId, It.IsAny<CancellationToken>()), Times.Once);
            _app.Payment.Verify(
                x => x.GenerarPreferenciaPagoAsync(It.IsAny<PedidoPagoDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
