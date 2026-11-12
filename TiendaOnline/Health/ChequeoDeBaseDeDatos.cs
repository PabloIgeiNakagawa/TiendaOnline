using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Health
{
    /// <summary>
    /// Health check del deploy y del monitor: la app está viva solo si puede
    /// leer la base — un SELECT 1 barato por request. SQL Server y SQLite
    /// aceptan la misma sintaxis, así que sirve en dev y en producción.
    /// El endpoint queda anónimo a propósito: es lo que golpea el pipeline de
    /// deploy y cualquier monitor externo (sin cookie, sin sesión).
    /// </summary>
    public sealed class ChequeoDeBaseDeDatos : IHealthCheck
    {
        private readonly TiendaContext _context;

        public ChequeoDeBaseDeDatos(TiendaContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                return HealthCheckResult.Healthy("DB online");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("DB offline", ex);
            }
        }
    }
}
