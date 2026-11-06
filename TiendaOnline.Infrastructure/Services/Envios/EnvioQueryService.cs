using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Envios;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Envios
{
    public class EnvioQueryService : IEnvioQueryService
    {
        private readonly TiendaContext _context;
        private readonly ILogger<EnvioQueryService> _logger;

        public EnvioQueryService(TiendaContext context, ILogger<EnvioQueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CotizacionEnvioDto> CotizarEnvioAsync(string? provincia, CancellationToken ct = default)
        {
            var zonas = await _context.ZonasEnvio
                .AsNoTracking()
                .ToListAsync(ct);

            // Match en memoria: la tabla es chica (24 filas) y así el criterio es
            // insensible a mayúsculas/espacios igual en SQL Server y en los tests de SQLite.
            var zona = zonas.FirstOrDefault(z =>
                string.Equals(z.Provincia?.Trim(), provincia?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (zona == null)
            {
                // Provincia fuera de la tabla (seed incompleto o cadena inesperada):
                // cotizamos al costo inicial en vez de frenar la compra; el Warning deja
                // el rastro para cargar la fila que falta.
                if (!string.IsNullOrWhiteSpace(provincia))
                {
                    _logger.LogWarning("Zona de envío inexistente para la provincia {Provincia}. Se usa el costo inicial por defecto.", provincia);
                }
                return new CotizacionEnvioDto(DbInitializer.CostoEnvioInicial, Permitida: true, EsCostoDefecto: true);
            }

            return new CotizacionEnvioDto(zona.Costo, Permitida: zona.Activa, EsCostoDefecto: false);
        }

        public async Task<List<ZonaEnvioDto>> ObtenerZonasAsync(CancellationToken ct = default)
        {
            return await _context.ZonasEnvio
                .AsNoTracking()
                .OrderBy(z => z.Provincia)
                .Select(z => new ZonaEnvioDto(z.ZonaEnvioId, z.Provincia, z.Costo, z.Activa))
                .ToListAsync(ct);
        }
    }
}
