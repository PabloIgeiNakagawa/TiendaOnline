using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Usuarios;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Usuarios
{
    /// <summary>
    /// Tests de CambiarPasswordAsync sobre TiendaContext real (SQLite): el hasher
    /// real (PasswordHasher) verifica el roundtrip actual → nueva, el email sale
    /// por mock para capturar el aviso.
    /// </summary>
    public class UsuarioCommandServiceTests
    {
        private const string Actual = "ClaveVieja1!";
        private const string Nueva = "ClaveNueva2!";

        private readonly Mock<IEmailService> _email = new();

        private UsuarioCommandService CrearServicio(TiendaContext ctx)
            => new(ctx, new PasswordHasher<Usuario>(), _email.Object,
                NullLogger<UsuarioCommandService>.Instance);

        /// <summary>Siembra un usuario con la contraseña actual hasheada de verdad.</summary>
        private static Usuario SeedConHash(TiendaContext ctx, string contrasenaActual)
        {
            var usuario = TestDatabase.SeedUsuario(ctx, email: "cambio@test.local");
            usuario.Contrasena = new PasswordHasher<Usuario>().HashPassword(usuario, contrasenaActual);
            ctx.SaveChanges();
            return usuario;
        }

        [Fact]
        public async Task CambiarPassword_ActualCorrecta_RehasheaLaNuevaYNotifica()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = SeedConHash(ctx, Actual);
            var hashAnterior = usuario.Contrasena;

            await CrearServicio(ctx).CambiarPasswordAsync(usuario.UsuarioId, Actual, Nueva);

            ctx.ChangeTracker.Clear();
            var guardado = ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == usuario.UsuarioId);
            Assert.NotEqual(hashAnterior, guardado.Contrasena);
            // La nueva verifica contra el hash recién guardado (roundtrip completo).
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<Usuario>().VerifyHashedPassword(guardado, guardado.Contrasena, Nueva));
            _email.Verify(x => x.EnviarEmailPasswordCambiadaAsync("cambio@test.local", "Test"), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_ActualIncorrecta_LanzaYNoTocaNada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = SeedConHash(ctx, Actual);
            var hashAnterior = usuario.Contrasena;

            var servicio = CrearServicio(ctx);

            var excepcion = await Assert.ThrowsAsync<PasswordActualIncorrectaException>(
                () => servicio.CambiarPasswordAsync(usuario.UsuarioId, "Contraseña-equivocada", Nueva));

            // Mensaje curado para UI (regla de las DomainException).
            Assert.Equal("La contraseña actual no es correcta.", excepcion.Message);

            ctx.ChangeTracker.Clear();
            var guardado = ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == usuario.UsuarioId);
            Assert.Equal(hashAnterior, guardado.Contrasena);
            _email.Verify(x => x.EnviarEmailPasswordCambiadaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CambiarPassword_EmailDeAvisoFalla_ElCambioQuedaAplicado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = SeedConHash(ctx, Actual);

            _email.Setup(x => x.EnviarEmailPasswordCambiadaAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Resend caído"));

            await CrearServicio(ctx).CambiarPasswordAsync(usuario.UsuarioId, Actual, Nueva);

            ctx.ChangeTracker.Clear();
            var guardado = ctx.Usuarios.AsNoTracking().Single(u => u.UsuarioId == usuario.UsuarioId);
            // Regla de la casa: el email es best-effort y va después del commit.
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<Usuario>().VerifyHashedPassword(guardado, guardado.Contrasena, Nueva));
        }
    }
}
