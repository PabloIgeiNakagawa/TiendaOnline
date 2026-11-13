using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Auth;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Auth
{
    /// <summary>
    /// Tests de RecuperacionPasswordService sobre TiendaContext real (SQLite) y
    /// EphemeralDataProtectionProvider (el token real de DataProtection, solo que
    /// sin keys en disco). El email sale por mock: se captura el token que viaja
    /// en el link para validar el roundtrip completo.
    /// </summary>
    public class RecuperacionPasswordServiceTests
    {
        private const string Password = "Pass1234!";

        private readonly Mock<IPasswordHasher<Usuario>> _hasher = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly IDataProtectionProvider _proteccion = new EphemeralDataProtectionProvider();

        private RecuperacionPasswordService CrearServicio(TiendaContext ctx, TimeSpan? vida = null)
            => new(ctx, _proteccion, _hasher.Object, _email.Object,
                NullLogger<RecuperacionPasswordService>.Instance, vida);

        /// <summary>Pide el reset y devuelve el token capturado del email (lo que iría en el link).</summary>
        private async Task<string?> PedirResetYCapturarTokenAsync(TiendaContext ctx, string email, TimeSpan? vida = null)
        {
            string? tokenCapturado = null;
            _email.Setup(x => x.EnviarEmailResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, _, token) => tokenCapturado = token)
                .Returns(Task.CompletedTask);

            var sut = CrearServicio(ctx, vida);
            await sut.PedirResetPasswordAsync(email);

            return tokenCapturado;
        }

        // ------------------------------------------------------------------
        // PedirResetPasswordAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task PedirReset_EmailDeCuentaActiva_EnviaEmailConTokenValido()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, email: "user@test.local");

            var token = await PedirResetYCapturarTokenAsync(ctx, "user@test.local");

            Assert.False(string.IsNullOrEmpty(token));
            _email.Verify(
                x => x.EnviarEmailResetPasswordAsync("user@test.local", usuario.Nombre, It.IsAny<string>()),
                Times.Once);

            var sut = CrearServicio(ctx);
            Assert.Equal(usuario.UsuarioId, await sut.ValidarTokenResetAsync(token!));
        }

        [Fact]
        public async Task PedirReset_EmailInexistente_NoEnviaNada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "otro@test.local");

            await PedirResetYCapturarTokenAsync(ctx, "desconocido@test.local");

            // Anti-enumeración: sin cuenta no hay email (el caller responde genérico).
            _email.Verify(
                x => x.EnviarEmailResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task PedirReset_UsuarioInactivo_NoEnviaNada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "baja@test.local");
            var usuario = await ctx.Usuarios.FirstAsync(u => u.Email == "baja@test.local");
            usuario.Activo = false;
            await ctx.SaveChangesAsync();

            await PedirResetYCapturarTokenAsync(ctx, "baja@test.local");

            _email.Verify(
                x => x.EnviarEmailResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        // ------------------------------------------------------------------
        // ValidarTokenResetAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task ValidarToken_Malformado_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = CrearServicio(ctx);

            Assert.Null(await sut.ValidarTokenResetAsync(""));
            Assert.Null(await sut.ValidarTokenResetAsync("no-es-un-token"));
        }

        [Fact]
        public async Task ValidarToken_TokenAlterado_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "user@test.local");
            var token = await PedirResetYCapturarTokenAsync(ctx, "user@test.local");

            var alterado = token![0] == 'A' ? "B" + token[1..] : "A" + token[1..];

            var sut = CrearServicio(ctx);
            Assert.Null(await sut.ValidarTokenResetAsync(alterado));
        }

        [Fact]
        public async Task ValidarToken_TokenExpirado_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "user@test.local");

            // Vida negativa al GENERAR = token que ya nació vencido (sin esperar el reloj).
            var token = await PedirResetYCapturarTokenAsync(ctx, "user@test.local", vida: TimeSpan.FromSeconds(-1));
            Assert.False(string.IsNullOrEmpty(token));

            var sut = CrearServicio(ctx);
            Assert.Null(await sut.ValidarTokenResetAsync(token!));
        }

        [Fact]
        public async Task ValidarToken_TrasCambioDePassword_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "user@test.local");
            var token = await PedirResetYCapturarTokenAsync(ctx, "user@test.local");

            // El usuario cambió su contraseña por otra vía (reset previo, perfil):
            // el token viejo muere aunque los 30 minutos no hayan vencido.
            var usuario = await ctx.Usuarios.FirstAsync(u => u.Email == "user@test.local");
            usuario.Contrasena = "hash-distinto";
            await ctx.SaveChangesAsync();

            var sut = CrearServicio(ctx);
            Assert.Null(await sut.ValidarTokenResetAsync(token!));
        }

        // ------------------------------------------------------------------
        // ResetPasswordAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task ResetPassword_TokenValido_CambiaHashYNotifica()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, email: "user@test.local");
            var token = await PedirResetYCapturarTokenAsync(ctx, "user@test.local");

            _hasher.Setup(x => x.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
                .Returns("hash-nuevo");
            var sut = CrearServicio(ctx);

            var cambio = await sut.ResetPasswordAsync(token!, "NuevaClave123!");

            Assert.True(cambio);
            using var verify = db.CreateContext();
            var guardado = await verify.Usuarios.AsNoTracking().SingleAsync(u => u.UsuarioId == usuario.UsuarioId);
            Assert.Equal("hash-nuevo", guardado.Contrasena);
            _email.Verify(
                x => x.EnviarEmailPasswordCambiadaAsync("user@test.local", usuario.Nombre),
                Times.Once);
        }

        [Fact]
        public async Task ResetPassword_TokenInvalido_NoTocaLaBase()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, email: "user@test.local");
            var sut = CrearServicio(ctx);

            var cambio = await sut.ResetPasswordAsync("token-forjado", "NuevaClave123!");

            Assert.False(cambio);
            _hasher.Verify(x => x.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()), Times.Never);
            using var verify = db.CreateContext();
            var guardado = await verify.Usuarios.AsNoTracking().SingleAsync(u => u.UsuarioId == usuario.UsuarioId);
            Assert.Equal("hash-fake", guardado.Contrasena); // sin tocar
        }

        [Fact]
        public async Task ResetPassword_AvisoPorEmailFalla_ElCambioQuedaIgual()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, email: "user@test.local");
            var token = await PedirResetYCapturarTokenAsync(ctx, "user@test.local");

            _hasher.Setup(x => x.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
                .Returns("hash-nuevo");
            // El aviso explota (Resend caído): no deshace el cambio de contraseña.
            _email.Setup(x => x.EnviarEmailPasswordCambiadaAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Resend caído"));
            var sut = CrearServicio(ctx);

            var cambio = await sut.ResetPasswordAsync(token!, "NuevaClave123!");

            Assert.True(cambio);
            using var verify = db.CreateContext();
            var guardado = await verify.Usuarios.AsNoTracking().SingleAsync(u => u.UsuarioId == usuario.UsuarioId);
            Assert.Equal("hash-nuevo", guardado.Contrasena);
        }
    }
}
