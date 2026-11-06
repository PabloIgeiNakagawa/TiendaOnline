using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Envios;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Envios
{
    public class EnvioCommandService : IEnvioCommandService
    {
        private readonly TiendaContext _context;
        private readonly ILogger<EnvioCommandService> _logger;

        public EnvioCommandService(TiendaContext context, ILogger<EnvioCommandService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task GuardarZonaAsync(int zonaEnvioId, decimal costo, bool activa, CancellationToken ct = default)
        {
            if (costo < 0)
                throw new EnvioNoValidoException("El costo de envío no puede ser negativo.");

            var zona = await _context.ZonasEnvio
                .FirstOrDefaultAsync(z => z.ZonaEnvioId == zonaEnvioId, ct);

            if (zona == null)
                throw new EnvioNoValidoException("No se encontró la zona de envío.");

            zona.Costo = costo;
            zona.Activa = activa;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Zona de envío {ZonaEnvioId} guardada: {Provincia} → {Costo} (Activa: {Activa})",
                zona.ZonaEnvioId, zona.Provincia, zona.Costo, zona.Activa);
        }
    }
}
