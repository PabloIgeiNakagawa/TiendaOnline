using System.Net;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Export CSV del panel (GET /admin/pedidos/Exportar): descarga con BOM UTF-8,
    /// cabecera y filas legibles, filtros del Listado aplicados y 403 fuera del rol.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class ExportCsvIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public ExportCsvIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Exportar_ComoAdmin_DescargaCsvConCabeceraYFilas()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "export-admin@test.local", Rol.Administrador);
                var cliente = TestDatabase.SeedUsuario(ctx, "export-cliente1@test.local");
                var otroCliente = TestDatabase.SeedUsuario(ctx, "export-cliente2@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido1 = TestDatabase.SeedPedido(ctx, cliente, producto, 2, estado: EstadoPedido.Nuevo);
                var pedido2 = TestDatabase.SeedPedido(ctx, otroCliente, producto, 1, estado: EstadoPedido.Cancelado);
                return (AdminId: admin.UsuarioId, P1: pedido1.PedidoId, P2: pedido2.PedidoId);
            });

            var client = _app.CreateClientAs(ids.AdminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/admin/pedidos/Exportar");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.StartsWith("text/csv", respuesta.Content.Headers.ContentType?.ToString());
            Assert.Contains("attachment", respuesta.Content.Headers.ContentDisposition?.ToString());

            var csv = await respuesta.Content.ReadAsStringAsync();
            // BOM UTF-8 primero (sin él Excel adivina ANSI y rompe los acentos).
            Assert.StartsWith("\uFEFF", csv);
            Assert.Contains("Pedido;Fecha;Cliente;Email;Estado;Estado del pago;Método de pago;Total;Fecha de entrega;Comprobante", csv);
            // Los dos pedidos sembrados viajan, con sus estados legibles.
            Assert.Contains("export-cliente1@test.local", csv);
            Assert.Contains("export-cliente2@test.local", csv);
            Assert.Contains("Nuevo", csv);
            Assert.Contains("Cancelado", csv);
            // Separador ";": la fila del pedido 1 aparece con su id.
            Assert.Contains(ids.P1.ToString(), csv);
            Assert.Contains(ids.P2.ToString(), csv);
        }

        [Fact]
        public async Task Exportar_FiltroDeEstado_AplicaElMismoFiltroDelListado()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "export-filtro-admin@test.local", Rol.Administrador);
                var cliente = TestDatabase.SeedUsuario(ctx, "export-filtro-nuevo@test.local");
                var cancelado = TestDatabase.SeedUsuario(ctx, "export-filtro-cancelado@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                TestDatabase.SeedPedido(ctx, cliente, producto, 1, estado: EstadoPedido.Nuevo);
                TestDatabase.SeedPedido(ctx, cancelado, producto, 1, estado: EstadoPedido.Cancelado);
                return (AdminId: admin.UsuarioId, Cancelado: cancelado.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.AdminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/admin/pedidos/Exportar?EstadoId=4"); // 4 = Cancelado

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var csv = await respuesta.Content.ReadAsStringAsync();
            Assert.Contains("export-filtro-cancelado@test.local", csv);
            Assert.DoesNotContain("export-filtro-nuevo@test.local", csv);
        }

        [Fact]
        public async Task Exportar_UsuarioComun_PoliticaRechazaCon403()
        {
            _app.ResetMocks();
            var usuarioId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "export-comun@test.local").UsuarioId);

            var client = _app.CreateClientAs(usuarioId);
            var respuesta = await client.GetAsync("/admin/pedidos/Exportar");

            // La clase del controller es [Authorize(Roles = "Administrador")].
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }
    }
}
