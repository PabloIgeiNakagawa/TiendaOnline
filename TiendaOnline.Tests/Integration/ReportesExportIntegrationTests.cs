using System.IO.Compression;
using System.Net;
using System.Text;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Export de reportes del dashboard (GET /admin/AdminReportes/Exportar):
    /// CSV individual con BOM y formato es-AR, ZIP con una tabla por reporte,
    /// 403 fuera del rol y 404 para una clave inexistente.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class ReportesExportIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public ReportesExportIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Exportar_ReporteIndividual_ComoAdmin_DescargaCsvConBom()
        {
            _app.ResetMocks();
            var adminId = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "reportes-admin@test.local", Rol.Administrador);
                var cliente = TestDatabase.SeedUsuario(ctx, "reportes-cliente@test.local");
                var producto = TestDatabase.SeedProducto(ctx, precio: 250m);
                TestDatabase.SeedPedido(ctx, cliente, producto, cantidad: 2,
                    estado: EstadoPedido.Entregado, estadoPago: EstadoPago.Aprobado);
                return admin.UsuarioId;
            });

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/admin/AdminReportes/Exportar?reporte=TopClientes");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.StartsWith("text/csv", respuesta.Content.Headers.ContentType?.ToString());
            Assert.Contains("reporte-top-clientes-", respuesta.Content.Headers.ContentDisposition?.ToString());
            Assert.Contains(".csv", respuesta.Content.Headers.ContentDisposition?.ToString());

            var bytes = await respuesta.Content.ReadAsByteArrayAsync();
            // BOM UTF-8 primero (sin él Excel adivina ANSI y rompe los acentos):
            // se compara a nivel bytes porque la comparación de strings ignora U+FEFF.
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());

            var csv = Encoding.UTF8.GetString(bytes);
            Assert.Contains("Cliente;Email;Pedidos;Total gastado;Última compra", csv);
            Assert.Contains("Test User;reportes-cliente@test.local;1;500,00;", csv);
        }

        [Fact]
        public async Task Exportar_Todos_ComoAdmin_DescargaZipConUnaTablaPorReporte()
        {
            _app.ResetMocks();
            var adminId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "reportes-zip@test.local", Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/admin/AdminReportes/Exportar");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("application/zip", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.Contains("reportes-", respuesta.Content.Headers.ContentDisposition?.ToString());
            Assert.Contains(".zip", respuesta.Content.Headers.ContentDisposition?.ToString());

            var bytes = await respuesta.Content.ReadAsByteArrayAsync();
            using var archivo = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);

            Assert.Equal(9, archivo.Entries.Count);
            Assert.Equal("01-resumen.csv", archivo.Entries[0].Name);
            Assert.Equal("09-stock-inmovilizado.csv", archivo.Entries[^1].Name);
        }

        [Fact]
        public async Task Exportar_UsuarioComun_PoliticaRechazaCon403()
        {
            _app.ResetMocks();
            var usuarioId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "reportes-comun@test.local").UsuarioId);

            var client = _app.CreateClientAs(usuarioId);
            var respuesta = await client.GetAsync("/admin/AdminReportes/Exportar");

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }

        [Fact]
        public async Task Exportar_ReporteInexistente_Devuelve404()
        {
            _app.ResetMocks();
            var adminId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "reportes-404@test.local", Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/admin/AdminReportes/Exportar?reporte=NoExiste");

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        }
    }
}
