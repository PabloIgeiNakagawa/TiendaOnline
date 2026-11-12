using Microsoft.Extensions.Diagnostics.HealthChecks;
using TiendaOnline.Health;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Web
{
    /// <summary>
    /// Health check del deploy: la app responde Healthy solo con la base
    /// legible (SELECT 1). El caso Unhealthy es el que hace fallar el paso
    /// final del deploy si la DB no está disponible tras subir la app.
    /// </summary>
    public class ChequeoDeBaseDeDatosTests
    {
        [Fact]
        public async Task CheckHealthAsync_BaseDisponible_RetornaHealthy()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new ChequeoDeBaseDeDatos(ctx);

            var resultado = await sut.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, resultado.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_BaseNoDisponible_RetornaUnhealthySinLanzar()
        {
            // Contexto y base desechados: el SELECT 1 explota y el check lo
            // convierte en Unhealthy (jamás lanza — el pipeline de health
            // espera un resultado, no una excepción).
            var db = new TestDatabase();
            var ctx = db.CreateContext();
            var sut = new ChequeoDeBaseDeDatos(ctx);
            await ctx.DisposeAsync();
            db.Dispose();

            var resultado = await sut.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
            Assert.NotNull(resultado.Exception);
        }
    }
}
