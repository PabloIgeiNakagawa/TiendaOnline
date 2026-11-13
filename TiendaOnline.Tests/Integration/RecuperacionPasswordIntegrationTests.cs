using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TiendaOnline.Domain.Entities;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Recuperación de contraseña punta a punta contra la app real: pedir reset
    /// por POST (antiforgery real), capturar el token del email mockeado, abrir
    /// el enlace, aplicar la nueva contraseña y loguear con ella. La respuesta
    /// del pedido es genérica: email inexistente = mismo redirect, cero emails.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class RecuperacionPasswordIntegrationTests : IClassFixture<TestApp>
    {
        private const string PasswordVieja = "Vieja123!";
        private const string PasswordNueva = "NuevaClave123!";

        private readonly TestApp _app;

        public RecuperacionPasswordIntegrationTests(TestApp app)
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

        [Fact]
        public async Task FlujoCompleto_OlvideMiPassword_HastaLoginConLaNueva()
        {
            _app.ResetMocks();

            string? tokenReset = null;
            _app.Email.Setup(x => x.EnviarEmailResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, _, token) => tokenReset = token)
                .Returns(Task.CompletedTask);

            var email = $"reset-{Guid.NewGuid():N}".Substring(0, 16) + "@test.local";
            _app.WithDb(ctx =>
            {
                var hasher = new PasswordHasher<Usuario>();
                ctx.Usuarios.Add(new Usuario
                {
                    Nombre = "Test",
                    Apellido = "User",
                    Email = email,
                    Contrasena = hasher.HashPassword(new Usuario(), PasswordVieja),
                    Rol = Rol.Usuario
                });
                ctx.SaveChanges();
                return true;
            });

            var client = _app.CreateAnonymousClient();

            // 1. GET del form de pedir reset (entrega cookie + token de antiforgery)
            var paginaPedido = await client.GetAsync("/Accounts/OlvideMiPassword");
            Assert.Equal(HttpStatusCode.OK, paginaPedido.StatusCode);
            var bodyPedido = await paginaPedido.Content.ReadAsStringAsync();

            // 2. POST del pedido: redirect a Login + email con el token del enlace
            var postPedido = await client.PostAsync("/Accounts/OlvideMiPassword", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = ExtraerTokenAntiforgery(bodyPedido),
                    ["Email"] = email
                }));
            Assert.Equal(HttpStatusCode.Redirect, postPedido.StatusCode);
            Assert.StartsWith("/Accounts/Login", postPedido.Headers.Location?.OriginalString);
            _app.Email.Verify(
                x => x.EnviarEmailResetPasswordAsync(email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
            Assert.False(string.IsNullOrEmpty(tokenReset), "El email de reset no trajo token.");

            // 3. El enlace del email abre el form de nueva contraseña
            var paginaReset = await client.GetAsync($"/Accounts/ResetPassword?token={Uri.EscapeDataString(tokenReset!)}");
            Assert.Equal(HttpStatusCode.OK, paginaReset.StatusCode);
            var htmlReset = await paginaReset.Content.ReadAsStringAsync();
            Assert.Contains("Nueva contraseña", htmlReset);

            // 4. POST de la nueva contraseña: redirect a Login + aviso de seguridad
            var postReset = await client.PostAsync("/Accounts/ResetPassword", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = ExtraerTokenAntiforgery(htmlReset),
                    ["Token"] = tokenReset!,
                    ["Contrasena"] = PasswordNueva,
                    ["ConfirmarContrasena"] = PasswordNueva
                }));
            Assert.Equal(HttpStatusCode.Redirect, postReset.StatusCode);
            Assert.StartsWith("/Accounts/Login", postReset.Headers.Location?.OriginalString);
            _app.Email.Verify(
                x => x.EnviarEmailPasswordCambiadaAsync(email, It.IsAny<string>()),
                Times.Once);

            // 5. La contraseña vieja ya no entra; la nueva loguea al Home del cliente
            var paginaLogin = await client.GetAsync("/Accounts/Login");
            Assert.Equal(HttpStatusCode.OK, paginaLogin.StatusCode);
            var bodyLogin1 = await paginaLogin.Content.ReadAsStringAsync();

            var loginVieja = await client.PostAsync("/Accounts/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = ExtraerTokenAntiforgery(bodyLogin1),
                    ["Email"] = email,
                    ["Contrasena"] = PasswordVieja
                }));
            // Login fallido: la vista se re-renderiza con el error (200, sin redirect).
            // (Razor encodea la ñ del mensaje: se busca el sufijo sin acentos.)
            Assert.Equal(HttpStatusCode.OK, loginVieja.StatusCode);
            var bodyLogin2 = await loginVieja.Content.ReadAsStringAsync();
            Assert.Contains("Email o contrase", bodyLogin2);
            Assert.Contains("incorrectas.", bodyLogin2);

            var loginNueva = await client.PostAsync("/Accounts/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = ExtraerTokenAntiforgery(bodyLogin1),
                    ["Email"] = email,
                    ["Contrasena"] = PasswordNueva
                }));
            Assert.Equal(HttpStatusCode.Redirect, loginNueva.StatusCode);
            Assert.Equal("/", loginNueva.Headers.Location?.OriginalString); // Home, no Login
        }

        [Fact]
        public async Task OlvideMiPassword_EmailInexistente_RespuestaGenerica()
        {
            _app.ResetMocks();
            var client = _app.CreateAnonymousClient();

            var pagina = await client.GetAsync("/Accounts/OlvideMiPassword");
            var post = await client.PostAsync("/Accounts/OlvideMiPassword", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = ExtraerTokenAntiforgery(await pagina.Content.ReadAsStringAsync()),
                    ["Email"] = $"nadie-{Guid.NewGuid():N}".Substring(0, 14) + "@test.local"
                }));

            // Anti-enumeración: el que busca emails se lleva el mismo redirect que
            // un usuario válido, y no salió ningún email.
            Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
            Assert.StartsWith("/Accounts/Login", post.Headers.Location?.OriginalString);
            _app.Email.Verify(
                x => x.EnviarEmailResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ResetPassword_TokenInvalido_RedirigeAPedirUnoNuevo()
        {
            var client = _app.CreateAnonymousClient();

            var respuesta = await client.GetAsync("/Accounts/ResetPassword?token=token-forjado");

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.StartsWith("/Accounts/OlvideMiPassword", respuesta.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task ResetPassword_SinTokenAntiforgery_Devuelve400()
        {
            var client = _app.CreateAnonymousClient();

            var post = await client.PostAsync("/Accounts/ResetPassword", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Token"] = "lo-que-sea",
                    ["Contrasena"] = PasswordNueva,
                    ["ConfirmarContrasena"] = PasswordNueva
                }));

            Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        }
    }
}
