using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Auth;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Services.Auth;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Auth
{
    /// <summary>
    /// Tests de AuthService sobre TiendaContext real (SQLite in-memory).
    /// IPasswordHasher se mockea: probar el algoritmo de hashing de Identity
    /// no aporta valor acá; lo que importa es la lógica de credenciales/registro.
    /// </summary>
    public class AuthServiceTests
    {
        private const string Password = "Pass1234!";

        // ------------------------------------------------------------------
        // ValidarCredencialesAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task ValidarCredenciales_UsuarioInexistente_RetornaNullSinVerificarHash()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            var sut = new AuthService(ctx, hasher.Object);

            var resultado = await sut.ValidarCredencialesAsync("nadie@test.local", Password);

            Assert.Null(resultado);
            // No recae en el hasher si el email no existe (no hay nadie a quien verificar).
            hasher.Verify(
                x => x.VerifyHashedPassword(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ValidarCredenciales_PasswordIncorrecta_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, email: "user@test.local");
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            hasher.Setup(x => x.VerifyHashedPassword(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(PasswordVerificationResult.Failed);
            var sut = new AuthService(ctx, hasher.Object);

            var resultado = await sut.ValidarCredencialesAsync("user@test.local", Password);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ValidarCredenciales_PasswordCorrecta_RetornaDtoConDatosYRol()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, email: "user@test.local"); // seed: Rol.Usuario
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            hasher.Setup(x => x.VerifyHashedPassword(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(PasswordVerificationResult.Success);
            var sut = new AuthService(ctx, hasher.Object);

            var resultado = await sut.ValidarCredencialesAsync("user@test.local", Password);

            Assert.NotNull(resultado);
            Assert.Equal(usuario.UsuarioId, resultado.UsuarioId);
            Assert.Equal("Test", resultado.Nombre);
            Assert.Equal("User", resultado.Apellido);
            Assert.Equal("user@test.local", resultado.Email);
            Assert.Equal("Usuario", resultado.Rol);
        }

        // ------------------------------------------------------------------
        // RegisterAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task Register_EmailDuplicado_LanzaEmailDuplicadoException()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "dup@test.local"); // ya existe
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            var sut = new AuthService(ctx, hasher.Object);
            var dto = Dto(" DUP@test.local ");

            await Assert.ThrowsAsync<EmailDuplicadoException>(() => sut.RegisterAsync(dto));

            // No crée un segundo usuario con ese email.
            Assert.Equal(1, await ctx.Usuarios.CountAsync(u => u.Email == "dup@test.local"));
        }

        [Fact]
        public async Task Register_UsuarioNuevo_HasheaPersisteYNormalizaEmail()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            hasher.Setup(x => x.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
                .Returns("hash-registrado");
            var sut = new AuthService(ctx, hasher.Object);
            var dto = Dto(" ULISES@Test.COM ");

            await sut.RegisterAsync(dto);

            // Fresh context: lo que quedó realmente en la base.
            using var verify = db.CreateContext();
            var guardado = await verify.Usuarios.AsNoTracking()
                .SingleAsync(u => u.Email == "ulises@test.com");

            Assert.Equal("hash-registrado", guardado.Contrasena); // hash del hasher, no la password cruda
            Assert.Equal("Ulises", guardado.Nombre);
            Assert.Equal("Pérez", guardado.Apellido);
            Assert.Equal("11-5555-5555", guardado.Telefono);
            Assert.Equal(Rol.Usuario, guardado.Rol); // RolId 0 por default
            Assert.Equal("ulises@test.com", guardado.Email); // normalizado: trim + minúsculas
        }

        [Fact]
        public async Task ValidarCredenciales_UsuarioInactivo_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedUsuario(ctx, email: "inactivo@test.local");
            var usuario = await ctx.Usuarios.FirstAsync(u => u.Email == "inactivo@test.local");
            usuario.Activo = false; // lo "desactiva" el panel de admin
            await ctx.SaveChangesAsync();

            var hasher = new Mock<IPasswordHasher<Usuario>>();
            var sut = new AuthService(ctx, hasher.Object);

            var resultado = await sut.ValidarCredencialesAsync("inactivo@test.local", Password);

            Assert.Null(resultado); // el usuario deshabilitado no loguea (ni por password, ni por Google)
            hasher.Verify(
                x => x.VerifyHashedPassword(It.IsAny<Usuario>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Register_IgnoraRolDelCliente_PorDefectoRolUsuario()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            var sut = new AuthService(ctx, hasher.Object);
            var dto = Dto("forjado@test.local");
            dto.RolId = (int)Rol.Administrador; // rol forjado en el POST del cliente
            hasher.Setup(x => x.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
                .Returns("hash-forjado");

            await sut.RegisterAsync(dto); // sin permitirRolPersonalizado

            using var verify = db.CreateContext();
            var guardado = await verify.Usuarios.AsNoTracking()
                .SingleAsync(u => u.Email == "forjado@test.local");
            Assert.Equal(Rol.Usuario, guardado.Rol); // el servicio no confía en el cliente
        }

        [Fact]
        public async Task Register_ConPermisoExplicito_AplicaRolIndicado()
        {
            // La creación de usuarios con rol sigue existiendo para el flujo del panel
            // (AccountsController.Register pasa permitirRolPersonalizado: esAdmin).
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var hasher = new Mock<IPasswordHasher<Usuario>>();
            var sut = new AuthService(ctx, hasher.Object);
            var dto = Dto("admincreado@test.local");
            dto.RolId = (int)Rol.Administrador;
            hasher.Setup(x => x.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
                .Returns("hash-admin");

            await sut.RegisterAsync(dto, permitirRolPersonalizado: true);

            using var verify = db.CreateContext();
            var guardado = await verify.Usuarios.AsNoTracking()
                .SingleAsync(u => u.Email == "admincreado@test.local");
            Assert.Equal(Rol.Administrador, guardado.Rol);
        }

        private static RegisterDto Dto(string email)
            => new()
            {
                Nombre = "Ulises",
                Apellido = "Pérez",
                Email = email,
                Telefono = "11-5555-5555",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Direccion = "Calle Falsa 123",
                Contrasena = Password,
                ConfirmarContrasena = Password,
                RolId = 0 // Rol.Usuario
            };
    }
}
