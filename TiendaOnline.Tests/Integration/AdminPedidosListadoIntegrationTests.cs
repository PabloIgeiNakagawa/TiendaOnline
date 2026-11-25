using System.Net;
using System.Text.RegularExpressions;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Acciones del listado admin de pedidos por estado: "Enviar" solo existe
    /// desde EnPreparacion (el guard del service no deja enviar un Nuevo) y el
    /// cancelar de EnPreparacion viaja con data-cobrado porque ese estado
    /// siempre implica pago aprobado (cancelar = reembolsar).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class AdminPedidosListadoIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public AdminPedidosListadoIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Listado_PedidoNuevo_MuestraCancelarYNoEnviar()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "listado-nuevo-admin@test.local", Rol.Administrador);
                var cliente = TestDatabase.SeedUsuario(ctx, "listado-nuevo-cliente@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, cliente, producto, 1, estado: EstadoPedido.Nuevo);
                return (AdminId: admin.UsuarioId, PedidoId: pedido.PedidoId);
            });

            var html = await ObtenerListadoAsync(ids.AdminId, ids.PedidoId);

            var botones = BotonesDelPedido(html, ids.PedidoId);
            Assert.Contains(botones, b => b.Contains("data-accion=\"cancelar\""));
            // Un pedido Nuevo todavía no está pago: enviarlo lo dejaría fuera de
            // la máquina de estados; el service lo rechaza. El botón no se ofrece.
            Assert.DoesNotContain(botones, b => b.Contains("data-accion=\"enviar\""));
        }

        [Fact]
        public async Task Listado_PedidoEnPreparacion_MuestraEnviarYCancelarConAvisoDeReembolso()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "listado-preparacion-admin@test.local", Rol.Administrador);
                var cliente = TestDatabase.SeedUsuario(ctx, "listado-preparacion-cliente@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, cliente, producto, 1,
                    estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado);
                return (AdminId: admin.UsuarioId, PedidoId: pedido.PedidoId);
            });

            var html = await ObtenerListadoAsync(ids.AdminId, ids.PedidoId);

            var botones = BotonesDelPedido(html, ids.PedidoId);
            Assert.Contains(botones, b => b.Contains("data-accion=\"enviar\""));

            // EnPreparacion siempre está cobrado: el modal debe avisar el reembolso
            // antes de confirmar (mismo contrato que Detalles del pedido).
            var cancelar = Assert.Single(botones, b => b.Contains("data-accion=\"cancelar\""));
            Assert.Contains("data-cobrado=\"true\"", cancelar);
        }

        private async Task<string> ObtenerListadoAsync(int adminId, int pedidoId)
        {
            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            // Búsqueda por id: el listado pagina de a 10 sobre la base compartida
            // de la colección, y el pedido del test tiene que estar en la página.
            var respuesta = await client.GetAsync($"/admin/pedidos/Listado?busqueda={pedidoId}");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            return await respuesta.Content.ReadAsStringAsync();
        }

        private static List<string> BotonesDelPedido(string html, int pedidoId)
        {
            return Regex.Matches(html, "<button[^>]*>")
                .Select(m => m.Value)
                .Where(t => t.Contains($"data-id=\"{pedidoId}\""))
                .ToList();
        }
    }
}
