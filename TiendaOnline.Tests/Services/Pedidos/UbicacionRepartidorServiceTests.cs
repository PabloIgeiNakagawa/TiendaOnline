using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// La última posición del repartidor: una fila por usuario que se pisa en
    /// cada latido (sin historial) y se borra al detener el compartir. Coordenadas
    /// fuera de rango se rechazan antes de escribir.
    /// </summary>
    public class UbicacionRepartidorServiceTests
    {
        [Fact]
        public async Task Registrar_CoordenadaInvalida_LanzaYNoGuarda()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var repartidor = TestDatabase.SeedUsuario(ctx, rol: Rol.Repartidor);
            var sut = Sut.UbicacionRepartidor(ctx, repartidor.UsuarioId);

            await Assert.ThrowsAsync<UbicacionNoValidaException>(
                () => sut.RegistrarAsync(91, -58, 10));

            using var verify = db.CreateContext();
            Assert.False(await verify.UbicacionesRepartidor.AnyAsync());
        }

        [Fact]
        public async Task Registrar_DosLatidos_DejaUnaSolaFilaConLaUltimaPosicion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var repartidor = TestDatabase.SeedUsuario(ctx, rol: Rol.Repartidor);
            var sut = Sut.UbicacionRepartidor(ctx, repartidor.UsuarioId);

            await sut.RegistrarAsync(-34.60, -58.38, 12);
            await sut.RegistrarAsync(-34.61, -58.39, 8);

            using var verify = db.CreateContext();
            var filas = await verify.UbicacionesRepartidor.AsNoTracking().ToListAsync();
            var fila = Assert.Single(filas);
            Assert.Equal(repartidor.UsuarioId, fila.UsuarioId);
            Assert.Equal(-34.61, fila.Latitud);
            Assert.Equal(-58.39, fila.Longitud);
            Assert.Equal(8, fila.PrecisionMetros);
        }

        [Fact]
        public async Task Detener_BorraLaFilaDelRepartidor()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var repartidor = TestDatabase.SeedUsuario(ctx, rol: Rol.Repartidor);
            var sut = Sut.UbicacionRepartidor(ctx, repartidor.UsuarioId);
            await sut.RegistrarAsync(-34.60, -58.38, null);

            await sut.DetenerAsync();

            using var verify = db.CreateContext();
            Assert.False(await verify.UbicacionesRepartidor.AnyAsync());
        }

        [Fact]
        public async Task EstaCompartiendo_ReflejaLaFilaVigente()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var repartidor = TestDatabase.SeedUsuario(ctx, rol: Rol.Repartidor);
            var sut = Sut.UbicacionRepartidor(ctx, repartidor.UsuarioId);

            Assert.False(await sut.EstaCompartiendoAsync());

            await sut.RegistrarAsync(-34.60, -58.38, null);
            Assert.True(await sut.EstaCompartiendoAsync());
        }
    }
}
