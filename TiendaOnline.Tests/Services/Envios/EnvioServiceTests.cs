using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Envios;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Envios;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Envios
{
    /// <summary>
    /// Tests de la cotización de envío: la tarifa sale de la tabla de zonas que edita
    /// el admin, contra la DB real (SQLite in-memory). El match de provincia es
    /// insensible a mayúsculas y espacios.
    /// </summary>
    public class EnvioQueryServiceTests
    {
        [Fact]
        public async Task Cotizar_ZonaExistente_DevuelveCostoDeLaZona()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedZonaEnvio(ctx, "Córdoba", costo: 1200m);
            var sut = Sut.EnvioQuery(ctx);

            var cotizacion = await sut.CotizarEnvioAsync("Córdoba");

            Assert.True(cotizacion.Permitida);
            Assert.False(cotizacion.EsCostoDefecto);
            Assert.Equal(1200m, cotizacion.Costo);
        }

        [Fact]
        public async Task Cotizar_CasingYEspaciosDistintos_MatcheaIgual()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedZonaEnvio(ctx, "Buenos Aires", costo: 800m);
            var sut = Sut.EnvioQuery(ctx);

            // El cliente manda la provincia con casing distintos al seed.
            var cotizacion = await sut.CotizarEnvioAsync("  buenos aires  ");

            Assert.True(cotizacion.Permitida);
            Assert.False(cotizacion.EsCostoDefecto);
            Assert.Equal(800m, cotizacion.Costo);
        }

        [Fact]
        public async Task Cotizar_ProvinciaSinFila_UsaCostoInicialPorDefecto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedZonaEnvio(ctx, "Mendoza", costo: 900m); // otra zona, no esta
            var sut = Sut.EnvioQuery(ctx);

            var cotizacion = await sut.CotizarEnvioAsync("Catamarca");

            Assert.True(cotizacion.Permitida);
            Assert.True(cotizacion.EsCostoDefecto);
            Assert.Equal(DbInitializer.CostoEnvioInicial, cotizacion.Costo);
        }

        [Fact]
        public async Task Cotizar_ProvinciaNulaOVacia_CostoInicial()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = Sut.EnvioQuery(ctx);

            var nula = await sut.CotizarEnvioAsync(null);
            var vacia = await sut.CotizarEnvioAsync("");

            Assert.True(nula.Permitida);
            Assert.True(nula.EsCostoDefecto);
            Assert.Equal(DbInitializer.CostoEnvioInicial, nula.Costo);
            Assert.Equal(DbInitializer.CostoEnvioInicial, vacia.Costo);
        }

        [Fact]
        public async Task Cotizar_ZonaDesactivada_NoEstaPermitida()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedZonaEnvio(ctx, "Formosa", costo: 700m, activa: false);
            var sut = Sut.EnvioQuery(ctx);

            var cotizacion = await sut.CotizarEnvioAsync("Formosa");

            Assert.False(cotizacion.Permitida);
        }
    }

    public class EnvioCommandServiceTests
    {
        [Fact]
        public async Task Guardar_CambiaCostoYEstado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var zona = TestDatabase.SeedZonaEnvio(ctx, "Santa Fe", costo: 500m, activa: true);
            var sut = Sut.EnvioCommand(ctx);

            await sut.GuardarZonaAsync(zona.ZonaEnvioId, costo: 950m, activa: false);

            using var verify = db.CreateContext();
            var zonaDb = await verify.ZonasEnvio.AsNoTracking().SingleAsync(z => z.ZonaEnvioId == zona.ZonaEnvioId);
            Assert.Equal(950m, zonaDb.Costo);
            Assert.False(zonaDb.Activa);
        }

        [Fact]
        public async Task Guardar_CostoCero_EsValido_EnvioGratis()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var zona = TestDatabase.SeedZonaEnvio(ctx, "Tucumán", costo: 500m);
            var sut = Sut.EnvioCommand(ctx);

            await sut.GuardarZonaAsync(zona.ZonaEnvioId, costo: 0m, activa: true);

            using var verify = db.CreateContext();
            var zonaDb = await verify.ZonasEnvio.AsNoTracking().SingleAsync(z => z.ZonaEnvioId == zona.ZonaEnvioId);
            Assert.Equal(0m, zonaDb.Costo);
        }

        [Fact]
        public async Task Guardar_CostoNegativo_LanzaEnvioNoValido()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var zona = TestDatabase.SeedZonaEnvio(ctx, "Salta", costo: 500m);
            var sut = Sut.EnvioCommand(ctx);

            await Assert.ThrowsAsync<EnvioNoValidoException>(
                () => sut.GuardarZonaAsync(zona.ZonaEnvioId, costo: -1m, activa: true));
        }

        [Fact]
        public async Task Guardar_ZonaInexistente_LanzaEnvioNoValido()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = Sut.EnvioCommand(ctx);

            await Assert.ThrowsAsync<EnvioNoValidoException>(
                () => sut.GuardarZonaAsync(999, costo: 500m, activa: true));
        }
    }
}
