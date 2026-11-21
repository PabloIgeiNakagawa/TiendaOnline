using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El destino post-login por rol (helper RedirigirSegunRol del
    /// AccountsController): admin al panel, reparto a su cola, cliente al Home —
    /// la prueba del cliente "/" ya vive en el flujo de recuperación de contraseña.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class LoginRedirectsIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public LoginRedirectsIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private const string Clave = "ClaveRedirect1!";

        private void SeedUsuarioConPassword(string rol, string email)
        {
            _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, email, Enum.Parse<Rol>(rol));
                usuario.Contrasena = new PasswordHasher<Usuario>().HashPassword(usuario, Clave);
                ctx.SaveChanges();
                return true;
            });
        }

        private async Task<string> LoginAsync(string email)
        {
            var client = _app.CreateAnonymousClient();
            var pagina = await client.GetAsync("/Accounts/Login");
            var post = await client.PostAsync("/Accounts/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync()),
                    ["Email"] = email,
                    ["Contrasena"] = Clave
                }));
            Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
            return post.Headers.Location?.OriginalString ?? "";
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
        public async Task Login_Admin_RedirigeAlPanel()
        {
            _app.ResetMocks();
            var email = $"login-admin-{Guid.NewGuid():N}@test.local";
            SeedUsuarioConPassword("Administrador", email);

            var destino = await LoginAsync(email);

            Assert.Equal("/admin", destino);
        }

        [Fact]
        public async Task Login_Repartidor_RedirigeALaCola()
        {
            _app.ResetMocks();
            var email = $"login-repartidor-{Guid.NewGuid():N}@test.local";
            SeedUsuarioConPassword("Repartidor", email);

            var destino = await LoginAsync(email);

            Assert.Equal("/Repartidor", destino);
        }
    }
}
