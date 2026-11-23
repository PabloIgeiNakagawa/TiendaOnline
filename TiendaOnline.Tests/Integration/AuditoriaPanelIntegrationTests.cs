using System.Net;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Panel de auditoría: el filtro por entidad acota el listado y el CSV, y el
    /// acceso "Ver historial" del detalle de pedido solo lo ve el Administrador.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class AuditoriaPanelIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public AuditoriaPanelIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Logs_ConFiltroDeEntidad_MuestraSoloEseHistorial()
        {
            _app.ResetMocks();
            var datos = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "audit-panel-admin@test.local", Rol.Administrador);
                // Ids únicos por corrida: la base de integración es compartida.
                var entidadA = Guid.NewGuid().ToString("N");
                var entidadB = Guid.NewGuid().ToString("N");
                TestDatabase.SeedAuditoria(ctx, admin, "Envió Pedido", "Pedido", entidadA);
                TestDatabase.SeedAuditoria(ctx, admin, "Entregó Pedido", "Pedido", entidadB);
                return (AdminId: admin.UsuarioId, A: entidadA, B: entidadB);
            });

            var client = _app.CreateClientAs(datos.AdminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync(
                $"/admin/auditorias/Logs?TablaAfectada=Pedido&EntidadId={datos.A}&TamanoPagina=100");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await respuesta.Content.ReadAsStringAsync();
            // La fila filtrada va (la entidad se muestra como #id) y la otra no:
            // el filtro viaja del link "Ver historial" hasta la query del listado.
            Assert.Contains($"#{datos.A}", html);
            Assert.DoesNotContain($"#{datos.B}", html);
            Assert.Contains("Pedido", html);
        }

        [Fact]
        public async Task Exportar_ComoAdmin_DescargaElHistorialFiltradoEnCsv()
        {
            _app.ResetMocks();
            var datos = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "audit-csv-admin@test.local", Rol.Administrador);
                var entidadA = Guid.NewGuid().ToString("N");
                TestDatabase.SeedAuditoria(ctx, admin, "Envió Pedido", "Pedido", entidadA);
                TestDatabase.SeedAuditoria(ctx, admin, "Entregó Pedido", "Pedido", Guid.NewGuid().ToString("N"));
                return (AdminId: admin.UsuarioId, A: entidadA);
            });

            var client = _app.CreateClientAs(datos.AdminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync(
                $"/admin/auditorias/Exportar?TablaAfectada=Pedido&EntidadId={datos.A}");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.StartsWith("text/csv", respuesta.Content.Headers.ContentType?.ToString());
            Assert.Contains("attachment", respuesta.Content.Headers.ContentDisposition?.ToString());

            var csv = await respuesta.Content.ReadAsStringAsync();
            Assert.StartsWith("\uFEFF", csv); // BOM para Excel
            Assert.Contains("Fecha;Usuario;Email;Acción;Entidad;ID", csv);
            Assert.Contains(datos.A, csv);
            Assert.Contains("Envió Pedido", csv);
            Assert.DoesNotContain("Entregó Pedido", csv);
        }

        [Fact]
        public async Task Exportar_UsuarioComun_PoliticaRechazaCon403()
        {
            _app.ResetMocks();
            var usuarioId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "audit-csv-comun@test.local").UsuarioId);

            var client = _app.CreateClientAs(usuarioId);
            var respuesta = await client.GetAsync("/admin/auditorias/Exportar");

            // La clase del controller es [Authorize(Roles = "Administrador")].
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }

        [Fact]
        public async Task DetallesDelPedido_ElHistorialSoloLoVeElAdministrador()
        {
            _app.ResetMocks();
            var datos = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "audit-hist-admin@test.local", Rol.Administrador);
                var cliente = TestDatabase.SeedUsuario(ctx, "audit-hist-cliente@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, cliente, producto, 1,
                    estado: EstadoPedido.Entregado, estadoPago: EstadoPago.Aprobado);
                return (AdminId: admin.UsuarioId, ClienteId: cliente.UsuarioId, PedidoId: pedido.PedidoId);
            });

            var clientAdmin = _app.CreateClientAs(datos.AdminId, new[] { "Administrador" });
            var htmlAdmin = await (await clientAdmin.GetAsync($"/Pedidos/Detalles/{datos.PedidoId}"))
                .Content.ReadAsStringAsync();
            Assert.Contains("TablaAfectada=Pedido", htmlAdmin);
            Assert.Contains($"EntidadId={datos.PedidoId}", htmlAdmin);

            // El dueño ve el detalle, pero el link apunta al panel de admin: no viaja.
            var clientCliente = _app.CreateClientAs(datos.ClienteId);
            var htmlCliente = await (await clientCliente.GetAsync($"/Pedidos/Detalles/{datos.PedidoId}"))
                .Content.ReadAsStringAsync();
            Assert.DoesNotContain("TablaAfectada=Pedido", htmlCliente);
        }
    }
}
