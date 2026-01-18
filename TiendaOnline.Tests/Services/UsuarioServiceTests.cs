using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Data;
using TiendaOnline.Models;
using TiendaOnline.Services;
using TiendaOnline.Exceptions;
using Xunit;
using TiendaOnline.IServices;

namespace TiendaOnline.Tests.Services
{
    public class UsuarioServiceTests : IDisposable
    {
        private readonly TiendaContext _db;
        private readonly Mock<IAuditoriaService> _mockAuditoria;
        private readonly PasswordHasher<Usuario> _realHasher;
        private readonly UsuarioService _service;

        public UsuarioServiceTests()
        {
            // Configuración de Base de Datos en Memoria Única por Test
            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TiendaContext(options);
            _mockAuditoria = new Mock<IAuditoriaService>();
            _realHasher = new PasswordHasher<Usuario>();

            // Instancia del servicio con sus dependencias
            _service = new UsuarioService(_db, _realHasher, _mockAuditoria.Object);
        }

        [Fact]
        public async Task CrearUsuarioAsync_DebeGuardarUsuario_CuandoDatosSonValidos()
        {
            // ARRANGE
            var nuevoUsuario = new Usuario
            {
                Email = "nuevo@test.com",
                Nombre = "Juan",
                Apellido = "Perez",
                Telefono = "12345678",
                Contrasena = "Password123!"
            };

            // ACT
            await _service.CrearUsuarioAsync(nuevoUsuario);

            // ASSERT
            var usuarioEnDb = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == "nuevo@test.com");
            Assert.NotNull(usuarioEnDb);
            Assert.NotEqual("Password123!", usuarioEnDb.Contrasena); // Verificamos Hash
        }

        [Fact]
        public async Task CrearUsuarioAsync_DebeLanzarExcepcion_CuandoEmailYaExiste()
        {
            // ARRANGE
            string emailRepetido = "duplicado@test.com";
            _db.Usuarios.Add(new Usuario
            {
                Email = emailRepetido,
                Nombre = "Existente",
                Apellido = "User",
                Telefono = "000",
                Contrasena = "..."
            });
            await _db.SaveChangesAsync();

            var usuarioNuevo = new Usuario
            {
                Email = emailRepetido,
                Nombre = "Nuevo",
                Apellido = "Intento",
                Telefono = "111",
                Contrasena = "..."
            };

            // ACT & ASSERT
            await Assert.ThrowsAsync<EmailDuplicadoException>(() =>
                _service.CrearUsuarioAsync(usuarioNuevo)
            );
        }

        [Fact]
        public async Task DarBajaUsuarioAsync_DebePonerActivoEnFalse()
        {
            // ARRANGE
            var usuario = new Usuario
            {
                UsuarioId = 99,
                Email = "baja@test.com",
                Nombre = "Baja",
                Apellido = "Test",
                Telefono = "123",
                Contrasena = "...",
                Activo = true
            };
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            // ACT
            await _service.DarBajaUsuarioAsync(99);

            // ASSERT
            var resultado = await _db.Usuarios.FindAsync(99);
            Assert.False(resultado.Activo);
            Assert.NotNull(resultado.UltimaFechaBaja);

            // Verificamos que se llamó a la auditoría
            _mockAuditoria.Verify(a => a.RegistrarAccionAsync("Dar baja usuario", It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task EditarUsuarioAsync_DebeGuardarCambiosYRegistrarAuditoriaConDatosViejos()
        {
            // ARRANGE
            var original = new Usuario
            {
                UsuarioId = 5,
                Nombre = "Original",
                Apellido = "Perez",
                Email = "test@test.com",
                Telefono = "123",
                Contrasena = "..."
            };
            _db.Usuarios.Add(original);
            await _db.SaveChangesAsync();

            var cambios = new Usuario
            {
                Nombre = "Editado",
                Apellido = "Gomez",
                Telefono = "456"
            };

            // ACT
            await _service.EditarUsuarioAsync(5, cambios);

            // ASSERT
            var editado = await _db.Usuarios.FindAsync(5);
            Assert.Equal("Editado", editado.Nombre);

            // Verificamos que la auditoría recibió el nombre "Original" como estado anterior
            _mockAuditoria.Verify(a => a.RegistrarAccionAsync(
                "Editar usuario",
                It.Is<object>(old => old.ToString().Contains("Original")),
                It.IsAny<object>()
            ), Times.Once);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}