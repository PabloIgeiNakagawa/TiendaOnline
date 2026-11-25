using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Common.Settings;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Auditoria
{
    public class AuditoriaRetencionService : IAuditoriaRetencionService
    {
        // Lotes acotados: un DELETE único de todo el historial viejo bloquearía
        // la tabla y el log del panel por más tiempo del necesario.
        public const int TamanoLote = 1_000;

        private readonly TiendaContext _context;
        private readonly IOptions<AuditoriaSettings> _settings;
        private readonly ILogger<AuditoriaRetencionService> _logger;

        public AuditoriaRetencionService(
            TiendaContext context,
            IOptions<AuditoriaSettings> settings,
            ILogger<AuditoriaRetencionService> logger)
        {
            _context = context;
            _settings = settings;
            _logger = logger;
        }

        public async Task<int> PurgarAuditoriasAsync()
        {
            var meses = _settings.Value.RetencionMeses;
            if (meses <= 0)
            {
                // Una retención en cero o negativa borraría todo: se prefiere el default.
                _logger.LogWarning(
                    "Retención de auditorías inválida ({Meses}); se usa el default de {MesesDefault} meses.",
                    meses,
                    AuditoriaSettings.RetencionMesesPorDefecto);
                meses = AuditoriaSettings.RetencionMesesPorDefecto;
            }

            var corte = DateTime.Now.AddMonths(-meses);
            var total = 0;

            while (true)
            {
                var ids = await _context.Auditorias
                    .Where(a => a.Fecha < corte)
                    .OrderBy(a => a.AuditoriaId)
                    .Take(TamanoLote)
                    .Select(a => a.AuditoriaId)
                    .ToListAsync();

                if (ids.Count == 0)
                    break;

                total += await _context.Auditorias
                    .Where(a => ids.Contains(a.AuditoriaId))
                    .ExecuteDeleteAsync();

                if (ids.Count < TamanoLote)
                    break;
            }

            _logger.LogInformation(
                "Purga de auditorías: {Total} filas anteriores a {Corte:yyyy-MM-dd} eliminadas (retención {Meses} meses).",
                total,
                corte,
                meses);

            return total;
        }
    }
}
