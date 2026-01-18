using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;
using TiendaOnline.Data;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Services
{
    public class AuditoriaServiceTests : IDisposable
    {
        private readonly TiendaContext _db;
        private readonly Mock<IHttpContextAccessor> _mockHttpContext;
        private readonly AuditoriaService _service;

        public AuditoriaServiceTests()
        {
            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TiendaContext(options);
            _mockHttpContext = new Mock<IHttpContextAccessor>();

            // Configuramos el Mock para que simule un usuario logueado con ID 1
            var claims = new List<Claim> { new Claim("UsuarioId", "1") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var context = new DefaultHttpContext { User = user };

            _mockHttpContext.Setup(h => h.HttpContext).Returns(context);

            _service = new AuditoriaService(_db, _mockHttpContext.Object);
        }

        [Fact]
        public async Task RegistrarAccionAsync_DebeGuardarDatosEnJson()
        {
            // ARRANGE
            var datosViejos = new { Precio = 100 };
            var datosNuevos = new { Precio = 120 };
            string accion = "Actualizar Precio";

            // ACT
            await _service.RegistrarAccionAsync(accion, datosViejos, datosNuevos);

            // ASSERT
            var registro = await _db.Auditorias.FirstOrDefaultAsync();
            Assert.NotNull(registro);
            Assert.Equal(accion, registro.Accion);
            Assert.Equal(1, registro.UsuarioId); // El ID que configuramos en el Mock

            // Verificamos que los objetos se convirtieron a JSON correctamente
            Assert.Contains("100", registro.DatosAnteriores);
            Assert.Contains("120", registro.DatosNuevos);
        }

        [Fact]
        public async Task RegistrarAccionAsync_DebeManejarDatosNulos()
        {
            // ACT
            await _service.RegistrarAccionAsync("Login", null, null);

            // ASSERT
            var registro = await _db.Auditorias.FirstOrDefaultAsync();
            Assert.NotNull(registro);
            Assert.Equal("{}", registro.DatosAnteriores); // Tu código pone {} si es null
            Assert.Equal("{}", registro.DatosNuevos);
        }

        [Fact]
        public async Task RegistrarAccionAsync_DebeLanzarExcepcion_SiNoHayUsuarioLogueado()
        {
            // ARRANGE - Quitamos el usuario del contexto
            _mockHttpContext.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

            // ACT & ASSERT
            // Tu código en AuditoriaService lanza UnauthorizedAccessException si no encuentra el Claim
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RegistrarAccionAsync("Test", null, null));
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}