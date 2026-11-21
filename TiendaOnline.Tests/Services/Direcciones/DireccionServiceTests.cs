using Microsoft.Extensions.Logging.Abstractions;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Direcciones;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Direcciones
{
    /// <summary>
    /// Guard de ownership server-side del checkout: ObtenerDelUsuarioAsync solo
    /// devuelve la dirección cuando pertenece al usuario pedido. Un id que llega
    /// del POST (no es autorización) de otro usuario devuelve null y el controller
    /// informa amablemente — calle/piso/observaciones de la víctima quedan intactas.
    /// </summary>
    public class DireccionObtenerDelUsuarioTests
    {
        private static Direccion SeedDireccion(TiendaContext ctx, int usuarioId, string etiqueta = "Casa")
        {
            var direccion = new Direccion
            {
                UsuarioId = usuarioId,
                Etiqueta = etiqueta,
                Calle = "Rivadavia",
                Numero = "123",
                Localidad = "Córdoba",
                Provincia = "Córdoba",
                CodigoPostal = "5000",
                Activo = true
            };
            ctx.Direcciones.Add(direccion);
            ctx.SaveChanges();
            return direccion;
        }

        [Fact]
        public async Task ObtenerDelUsuarioAsync_DireccionPropia_DevuelveElDtoCompleto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, "dueno-dir@test.local");
            var direccion = SeedDireccion(ctx, usuario.UsuarioId);
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var dto = await sut.ObtenerDelUsuarioAsync(direccion.DireccionId, usuario.UsuarioId);

            Assert.NotNull(dto);
            Assert.Equal(direccion.DireccionId, dto.DireccionId);
            Assert.Equal(usuario.UsuarioId, dto.UsuarioId);
            Assert.Equal("Casa", dto.Etiqueta);
            Assert.Equal("Rivadavia", dto.Calle);
            Assert.Equal("Córdoba", dto.Provincia);
        }

        [Fact]
        public async Task ObtenerDelUsuarioAsync_DireccionDeOtroUsuario_DevuelveNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var dueno = TestDatabase.SeedUsuario(ctx, "dueño-ajena-dir@test.local");
            var ajeno = TestDatabase.SeedUsuario(ctx, "ajeno-ajena-dir@test.local");
            var direccion = SeedDireccion(ctx, dueno.UsuarioId);
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var dto = await sut.ObtenerDelUsuarioAsync(direccion.DireccionId, ajeno.UsuarioId);

            Assert.Null(dto);
        }

        [Fact]
        public async Task ObtenerDelUsuarioAsync_DireccionInexistente_DevuelveNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, "inexistentes-dir@test.local");
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var dto = await sut.ObtenerDelUsuarioAsync(9999, usuario.UsuarioId);

            Assert.Null(dto);
        }
    }

    /// <summary>
    /// Contrato de lectura/escritura del resto del ABM: ObtenerPorIdAsync responde
    /// null para lo inexistente (los controllers 404 y el policy handler niega) y
    /// los writes lanzan la DomainException con mensaje curado — antes eran
    /// InvalidOperationException que solo producían una 500 sin explicación.
    /// </summary>
    public class DireccionContratosTests
    {
        private static Direccion SeedDireccion(TiendaContext ctx, int usuarioId)
        {
            var direccion = new Direccion
            {
                UsuarioId = usuarioId,
                Etiqueta = "Casa",
                Calle = "Rivadavia",
                Numero = "123",
                Localidad = "Córdoba",
                Provincia = "Córdoba",
                CodigoPostal = "5000",
                Activo = true
            };
            ctx.Direcciones.Add(direccion);
            ctx.SaveChanges();
            return direccion;
        }

        [Fact]
        public async Task ObtenerPorIdAsync_DireccionExistente_DevuelveElDto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, "por-id-dir@test.local");
            var direccion = SeedDireccion(ctx, usuario.UsuarioId);
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var dto = await sut.ObtenerPorIdAsync(direccion.DireccionId);

            Assert.NotNull(dto);
            Assert.Equal(direccion.DireccionId, dto.DireccionId);
            Assert.Equal(usuario.UsuarioId, dto.UsuarioId);
        }

        [Fact]
        public async Task ObtenerPorIdAsync_DireccionInexistente_DevuelveNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var dto = await sut.ObtenerPorIdAsync(9999);

            Assert.Null(dto);
        }

        [Fact]
        public async Task ActualizarDireccionAsync_DireccionInexistente_LanzaDireccionNoEncontrada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var excepcion = await Assert.ThrowsAsync<DireccionNoEncontradaException>(
                () => sut.ActualizarDireccionAsync(9999, new DireccionDto()));

            Assert.Equal("La dirección indicada no existe.", excepcion.Message);
        }

        [Fact]
        public async Task EliminarDireccionAsync_DireccionInexistente_LanzaDireccionNoEncontrada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new DireccionService(ctx, NullLogger<DireccionService>.Instance);

            var excepcion = await Assert.ThrowsAsync<DireccionNoEncontradaException>(
                () => sut.EliminarDireccionAsync(9999));

            Assert.Equal("La dirección indicada no existe.", excepcion.Message);
        }
    }
}
