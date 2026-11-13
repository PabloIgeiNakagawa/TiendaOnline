using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Cambio de contraseña desde el perfil (GET/POST /Usuarios/CambiarPassword/{id}):
    /// flujo feliz contra el service real (hasher verdadero), guard de contraseña
    /// actual incorrecta, ownership por ruta y antiforgery real.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class CambiarPasswordIntegrationTests : IClassFixture<TestApp>
    {
        private const string ClaveActual = "ClaveActual1!";
        private const string ClaveNueva = "ClaveNueva2!";

        private readonly TestApp _app;

        public CambiarPasswordIntegrationTests(TestApp app)
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

        private static (int Id, string Email) SeedConHash(TestApp app, string email)
        {
            return app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, email: email);
                // Hash real: el POST verifica la contraseña actual contra este hash.
                usuario.Contrasena = new PasswordHasher<Usuario>().HashPassword(usuario, ClaveActual);
                ctx.SaveChanges();
                return (usuario.UsuarioId, usuario.Email);
            });
        }

        [Fact]
        public async Task CambiarPassword_FlujoFeliz_RehasheaYNotificaPorEmail()
        {
            _app.ResetMocks();
            var (id, email) = SeedConHash(_app, "cambio-feliz@test.local");

            var client = _app.CreateClientAs(id);
            var pagina = await client.GetAsync($"/Usuarios/CambiarPassword/{id}");
            Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
            var token = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync());

            var respuesta = await client.PostAsync($"/Usuarios/CambiarPassword/{id}", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["ContrasenaActual"] = ClaveActual,
                    ["ContrasenaNueva"] = ClaveNueva,
                    ["ConfirmarContrasena"] = ClaveNueva
                }));

            Assert.Equal(HttpStatusCode.Found, respuesta.StatusCode);
            Assert.Contains($"/Usuarios/Perfil/{id}", respuesta.Headers.Location?.ToString());

            var (hash, usuario) = _app.WithDb(ctx =>
            {
                var u = ctx.Usuarios.AsNoTracking().Single(x => x.UsuarioId == id);
                return (u.Contrasena, u);
            });
            // Roundtrip completo: la nueva clave verifica contra el hash guardado.
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, hash, ClaveNueva));
            _app.Email.Verify(x => x.EnviarEmailPasswordCambiadaAsync(email, "Test"), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_ActualIncorrecta_ErrorInlineYHashIntacto()
        {
            _app.ResetMocks();
            var (id, _) = SeedConHash(_app, "cambio-malactual@test.local");

            var client = _app.CreateClientAs(id);
            var pagina = await client.GetAsync($"/Usuarios/CambiarPassword/{id}");
            var token = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync());

            var respuesta = await client.PostAsync($"/Usuarios/CambiarPassword/{id}", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["ContrasenaActual"] = "NoEsMiClave9?",
                    ["ContrasenaNueva"] = ClaveNueva,
                    ["ConfirmarContrasena"] = ClaveNueva
                }));

            // El action re-renderiza el form con el error (no redirect: el mensaje
            // curado del service queda inline junto al form; los acentos van como
            // entidades HTML en el source, el fragmento es ASCII).
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await respuesta.Content.ReadAsStringAsync();
            Assert.Contains("actual no es correcta.", html);

            var usuario = _app.WithDb(ctx => ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == id));
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.Contrasena, ClaveActual));
            _app.Email.Verify(x => x.EnviarEmailPasswordCambiadaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CambiarPassword_RutaAjena_PoliticaRechazaCon403()
        {
            _app.ResetMocks();
            var (victimaId, _) = SeedConHash(_app, "cambio-victima@test.local");
            var (atacanteId, _) = SeedConHash(_app, "cambio-atacante@test.local");

            var client = _app.CreateClientAs(atacanteId);
            // El atacante toma token+cookie de SU PROPIA página (usuario real).
            var pagina = await client.GetAsync($"/Usuarios/CambiarPassword/{atacanteId}");
            var token = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync());

            var respuesta = await client.PostAsync($"/Usuarios/CambiarPassword/{victimaId}", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["ContrasenaActual"] = "loquesea",
                    ["ContrasenaNueva"] = "Hackeada1!",
                    ["ConfirmarContrasena"] = "Hackeada1!"
                }));

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            var victima = _app.WithDb(ctx => ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == victimaId));
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<Usuario>().VerifyHashedPassword(victima, victima.Contrasena, ClaveActual));
        }

        [Fact]
        public async Task CambiarPassword_SinAntiforgery_400()
        {
            _app.ResetMocks();
            var (id, _) = SeedConHash(_app, "cambio-sintoken@test.local");

            var client = _app.CreateClientAs(id);
            var respuesta = await client.PostAsync($"/Usuarios/CambiarPassword/{id}", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["ContrasenaActual"] = ClaveActual,
                    ["ContrasenaNueva"] = ClaveNueva,
                    ["ConfirmarContrasena"] = ClaveNueva
                }));

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }
    }
}
