using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// La edición de categorías de punta a punta contra la app real: form con los
    /// datos precargados, POST de nombre+padre con antiforgery, guard de nombre
    /// duplicado en el form y el bucle de jerarquía convertido en aviso amable
    /// (DomainExceptionFilter) sin grabar nada. El lápiz del Listado apunta acá:
    /// era una action inexistente (404) antes de la reactivación.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class CategoriasEdicionIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public CategoriasEdicionIntegrationTests(TestApp app)
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

        private static int SembrarAdmin(TestApp app, string sufijo)
        {
            return app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, $"cat-{sufijo}-{Guid.NewGuid():N}@test.local", Rol.Administrador).UsuarioId);
        }

        private static (int CategoriaId, int OtraId) SembrarPar(TestApp app, string sufijo)
        {
            return app.WithDb(ctx =>
            {
                var categoria = TestDatabase.SeedCategoria(ctx, $"Categoria {sufijo}");
                var otra = TestDatabase.SeedCategoria(ctx, $"Otra {sufijo}");
                return (categoria.CategoriaId, otra.CategoriaId);
            });
        }

        [Fact]
        public async Task Editar_CategoriaExistente_RenderizaFormularioPrecargado()
        {
            _app.ResetMocks();
            var (categoriaId, _) = SembrarPar(_app, "form");
            var idAdmin = SembrarAdmin(_app, "form");

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var response = await client.GetAsync($"/admin/categorias/Editar/{categoriaId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("value=\"Categoria form\"", html);
            Assert.Contains("Guardar Cambios", html);
        }

        [Fact]
        public async Task Editar_CategoriaInexistente_404()
        {
            _app.ResetMocks();
            var idAdmin = SembrarAdmin(_app, "404");

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var response = await client.GetAsync("/admin/categorias/Editar/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Editar_PostActualizaNombreYPadre_RedirigeAlListado()
        {
            _app.ResetMocks();
            var (categoriaId, otraId) = SembrarPar(_app, "post");
            var idAdmin = SembrarAdmin(_app, "post");

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var page = await client.GetAsync($"/admin/categorias/Editar/{categoriaId}");
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Nombre"] = "Renombrada por POST",
                ["CategoriaPadreId"] = otraId.ToString()
            };
            var response = await client.PostAsync($"/admin/categorias/Editar/{categoriaId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/admin/categorias/Listado", response.Headers.Location?.ToString() ?? string.Empty);

            var editada = _app.WithDb(ctx =>
                ctx.Categorias.AsNoTracking().First(c => c.CategoriaId == categoriaId));
            Assert.Equal("Renombrada por POST", editada.Nombre);
            Assert.Equal(otraId, editada.CategoriaPadreId);
        }

        [Fact]
        public async Task Editar_NombreDuplicado_VuelveAlFormularioConError()
        {
            _app.ResetMocks();
            var (categoriaId, _) = SembrarPar(_app, "dup");
            var idAdmin = SembrarAdmin(_app, "dup");

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var page = await client.GetAsync($"/admin/categorias/Editar/{categoriaId}");
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            // El nombre de la OTRA categoría del par: el guard excluye a la propia.
            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Nombre"] = "Otra dup"
            };
            var response = await client.PostAsync($"/admin/categorias/Editar/{categoriaId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            // El HtmlEncoder por defecto encodea la "í" de "categoría": se aserte
            // el prefijo ASCII del mensaje, que es lo estable del guard.
            Assert.Contains("Ya existe una categor", html);

            var intacta = _app.WithDb(ctx =>
                ctx.Categorias.AsNoTracking().First(c => c.CategoriaId == categoriaId));
            Assert.Equal("Categoria dup", intacta.Nombre);
        }

        [Fact]
        public async Task Editar_PadreGeneraBucle_RedirigeConAvisoSinGrabar()
        {
            // Abuela → hija: hacer que la abuela cuelgue de la hija sube como
            // CategoriaNoValidaException y el filtro global la convierte en
            // redirect con flash de error — la edición completa no se graba.
            _app.ResetMocks();
            var idAdmin = SembrarAdmin(_app, "bucle");

            var (abuelaId, hijaId) = _app.WithDb(ctx =>
            {
                var abuela = TestDatabase.SeedCategoria(ctx, "Abuela bucle");
                var hija = TestDatabase.SeedCategoria(ctx, "Hija bucle");
                hija.CategoriaPadreId = abuela.CategoriaId;
                ctx.SaveChanges();
                return (abuela.CategoriaId, hija.CategoriaId);
            });

            var client = _app.CreateClientAs(idAdmin, new[] { "Administrador" });
            var page = await client.GetAsync($"/admin/categorias/Editar/{abuelaId}");
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Nombre"] = "Abuela bucle reinsertada",
                ["CategoriaPadreId"] = hijaId.ToString()
            };
            var response = await client.PostAsync($"/admin/categorias/Editar/{abuelaId}", new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);

            var intacta = _app.WithDb(ctx =>
                ctx.Categorias.AsNoTracking().First(c => c.CategoriaId == abuelaId));
            Assert.Equal("Abuela bucle", intacta.Nombre);
            Assert.Null(intacta.CategoriaPadreId);
        }

        [Fact]
        public async Task Editar_SinRolAdministrador_403()
        {
            _app.ResetMocks();
            var (categoriaId, _) = SembrarPar(_app, "403");
            var idUsuario = _app.WithDb(ctx =>
                TestDatabase.SeedUsuario(ctx, $"cat-comun-{Guid.NewGuid():N}@test.local").UsuarioId);

            var client = _app.CreateClientAs(idUsuario, new[] { "Usuario" });
            var response = await client.GetAsync($"/admin/categorias/Editar/{categoriaId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
