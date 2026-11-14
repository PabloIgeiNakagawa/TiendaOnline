using System.Net;
using System.Text.RegularExpressions;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Pills de filtros activos (parcial compartido _FiltrosAplicados dentro de _ResumenResultados):
    /// la X de cada píldora reconstruye la query sin SU parámetro conservando los demás,
    /// y el Limpiar todo deja la página sin filtros. Guarda el refactor contra regresiones.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class FiltrosAplicadosIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public FiltrosAplicadosIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Movimientos_ConBusqueda_PillConXQueQuitaSoloEseFiltro()
        {
            _app.ResetMocks();
            var adminId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "pills-movimientos@test.local", Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/Admin/MovimientosStock/Movimientos?busqueda=algo");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await respuesta.Content.ReadAsStringAsync();
            // Razor escapa los acentos (&#250;): los asserts usan fragmentos ASCII-seguros
            Assert.Contains("Producto: <strong>algo</strong>", html);
            Assert.Contains("Limpiar todo", html);

            // La X de la única píldora navega sin el parámetro de búsqueda y a la página 1
            var href = HrefsDePills(html).Single();
            Assert.Equal("/Admin/MovimientosStock/Movimientos?pagina=1", href);
        }

        [Fact]
        public async Task ListadoPedidos_ConDosFiltros_CadaXLimpiaSoloElSuyo()
        {
            _app.ResetMocks();
            var adminId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "pills-pedidos@test.local", Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var respuesta = await client.GetAsync("/admin/pedidos/Listado?busqueda=algo&estadoId=1");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await respuesta.Content.ReadAsStringAsync();
            // Razor escapa los acentos (&#250;): los asserts usan fragmentos ASCII-seguros
            Assert.Contains("Producto: <strong>algo</strong>", html);
            Assert.Contains("Estado: <strong>", html);

            // Dos pills: la de búsqueda conserva el estado, la de estado conserva la búsqueda
            var hrefs = HrefsDePills(html);
            Assert.Equal(2, hrefs.Count);
            Assert.StartsWith("/admin/pedidos/Listado?", hrefs[0]);
            Assert.Contains("estadoId=1", hrefs[0]);
            Assert.DoesNotContain("busqueda=algo", hrefs[0]);
            Assert.Contains("busqueda=algo", hrefs[1]);
            Assert.DoesNotContain("estadoId=1", hrefs[1]);
        }

        [Fact]
        public async Task ListadosAdmin_ConFiltros_EnvuelvenElFormularioEnBottomSheet()
        {
            // Guard del patrón mobile: los 6 listados del panel abren sus filtros
            // como bottom sheet (offcanvas-md offcanvas-bottom) con el botón Filtros
            _app.ResetMocks();
            var adminId = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, "pills-panel@test.local", Rol.Administrador).UsuarioId);

            var client = _app.CreateClientAs(adminId, new[] { "Administrador" });
            var rutas = new[]
            {
                "/Admin/MovimientosStock/Movimientos",
                "/admin/pedidos/Listado",
                "/admin/categorias/Listado",
                "/admin/usuarios/Listado",
                "/admin/auditorias/Logs",
                "/admin/productos/Catalogo",
            };

            foreach (var ruta in rutas)
            {
                var respuesta = await client.GetAsync(ruta);
                Assert.True(respuesta.IsSuccessStatusCode, $"GET {ruta} → {respuesta.StatusCode}");
                var html = await respuesta.Content.ReadAsStringAsync();
                Assert.Contains("data-bs-toggle=\"offcanvas\"", html);
                // La hoja usa SOLO el modificador responsivo: la clase `offcanvas` pelada
                // es un drawer fijo/oculto en todos los anchos (ocultaba los filtros en
                // desktop); combinarlas es el bug que este guard no deja volver.
                Assert.Contains("offcanvas-md offcanvas-bottom filtros-panel", html);
                Assert.DoesNotContain("offcanvas offcanvas-md", html);
            }
        }

        // href de los anchors con title="Quitar filtro" (una píldora = un anchor)
        private static List<string> HrefsDePills(string html)
        {
            return html.Split("<a ")
                .Where(segmento => segmento.Contains("Quitar filtro"))
                .Select(segmento => Regex.Match(segmento, "href=\"([^\"]+)\"").Groups[1].Value)
                .ToList();
        }
    }
}
