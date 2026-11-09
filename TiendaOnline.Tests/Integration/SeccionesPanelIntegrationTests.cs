using System.Net;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Guard del caparazón TODO DE GOLPE: el envase admin/secciones/todas pega
    /// directo y trae TODAS las secciones (la clave data-seccion de cada pieza
    /// es el contrato con el reparto de site.js — si una pieza falta, el
    /// contenedor queda con el aviso de error en vez de contenido). El error de
    /// un widget se degrada en el front y es invisible para los tests, así que
    /// este es el único que pega al 500 de fondo.
    /// Regresión que fija: los viejos Select crudos contra parciales con
    /// @model List<...> reventaban 500 por el check de ViewDataDictionary,
    /// aunque la lista viniera vacía (pesa el tipo en tiempo de ejecución, no
    /// las filas).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class SeccionesPanelIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public SeccionesPanelIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static readonly string[] ClavesDeSeccion =
        {
            "estado-sistema",
            "resumen-diario",
            "productos-bajo-stock",
            "pedidos-estancados",
            "ultimos-cambios",
            "movimientos-stock",
            "pedidos-recientes"
        };

        [Fact]
        public async Task TodasLasSecciones_ConRolAdministrador_TraeLasSieteSecciones()
        {
            // Arrange: admin real + datos mínimos para que las secciones
            // rendericen filas (producto casi sin stock y pedido Nuevo).
            _app.ResetMocks();
            var idAdmin = _app.WithDb(ctx =>
            {
                // Email único por caso: la DB del host se comparte entre pruebas.
                var admin = TestDatabase.SeedUsuario(
                    ctx, $"panel-{Guid.NewGuid():N}@test.local", Rol.Administrador);
                var producto = TestDatabase.SeedProducto(ctx, stock: 1);
                TestDatabase.SeedPedido(ctx, admin, producto, 1, estado: EstadoPedido.Nuevo);
                return admin.UsuarioId;
            });

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });

            // Act & Assert: 200 y las siete claves de reparto presentes.
            var response = await client.GetAsync("/admin/secciones/todas");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var cuerpo = await response.Content.ReadAsStringAsync();
            foreach (var clave in ClavesDeSeccion)
            {
                Assert.Contains($"data-seccion=\"{clave}\"", cuerpo);
            }
        }

        [Fact]
        public async Task PedidosEstancados_ConRolAdministrador_Responden200()
        {
            // El paginador sigue vivo por su cuenta: la página que pide el
            // usuario viaja de a una por fetch, aparte del envase inicial.
            _app.ResetMocks();
            var idAdmin = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, $"panel-{Guid.NewGuid():N}@test.local", Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });

            var response = await client.GetAsync("/admin/pedidos-estancados?pagina=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
