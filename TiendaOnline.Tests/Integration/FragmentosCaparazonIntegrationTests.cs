using System.Net;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Regla del caparazón: cada fragmento del panel (los 6 endpoints
    /// admin/parciales/* y el paginado admin/pedidos-estancados) responde 200
    /// con el rol Administrador pegando el endpoint directo. El error de un
    /// widget se degrada en el front al aviso de reintento — invisible para
    /// los tests — así que este guard es el único que pega al 500 de fondo.
    /// Regresión que fija: dos Select devueltos crudos (el IEnumerable del LINQ,
    /// sin materializar) contra parciales que declaran @model List<...>
    /// reventaban 500 por el check de ViewDataDictionary, aunque la lista viniera
    /// vacía (lo que importa es el tipo en tiempo de ejecución, no las filas).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class FragmentosCaparazonIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public FragmentosCaparazonIntegrationTests(TestApp app)
        {
            _app = app;
        }

        public static TheoryData<string> RutasDeFragmentos => new()
        {
            "/admin/parciales/estado-sistema",
            "/admin/parciales/resumen-diario",
            "/admin/parciales/ultimos-cambios",
            "/admin/parciales/movimientos-stock",
            "/admin/parciales/productos-bajo-stock",
            "/admin/parciales/pedidos-recientes",
            "/admin/pedidos-estancados"
        };

        [Theory]
        [MemberData(nameof(RutasDeFragmentos))]
        public async Task FragmentosDelPanel_ConRolAdministrador_Responden200(string ruta)
        {
            // Arrange: admin real + datos mínimos para que los parciales rendericen
            // filas (producto casi sin stock y pedido Nuevo).
            _app.ResetMocks();
            var idAdmin = _app.WithDb(ctx =>
            {
                // Email único por caso: la DB del host se comparte entre los 7
                // fetches de la Theory (el UNIQUE de Usuarios.Email no perdona).
                var admin = TestDatabase.SeedUsuario(
                    ctx, $"panel-{Guid.NewGuid():N}@test.local", Rol.Administrador);
                var producto = TestDatabase.SeedProducto(ctx, stock: 1);
                TestDatabase.SeedPedido(ctx, admin, producto, 1, estado: EstadoPedido.Nuevo);
                return admin.UsuarioId;
            });

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });

            // Act & Assert
            var response = await client.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
