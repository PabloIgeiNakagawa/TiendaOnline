using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Geo;

namespace TiendaOnline.Infrastructure.Services.Geo
{
    public class GeoService : IGeoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeoService> _logger;

        public GeoService(IHttpClientFactory httpClientFactory, ILogger<GeoService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<IEnumerable<LocationDto>> GetProvinciasAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.GetFromJsonAsync<GeorefResponse>("https://apis.datos.gob.ar/georef/api/provincias?campos=id,nombre", ct);
            return response?.Provincias.OrderBy(p => p.Nombre) ?? Enumerable.Empty<LocationDto>();
        }

        public async Task<IEnumerable<LocationDto>> GetLocalidadesAsync(string provinciaNombre, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(provinciaNombre))
                return Enumerable.Empty<LocationDto>();

            var provinciaEscapada = Uri.EscapeDataString(provinciaNombre);

            var url = $"https://apis.datos.gob.ar/georef/api/localidades?provincia={provinciaEscapada}&max=1000&campos=id,nombre";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<GeorefResponse>(url, ct);

                return response?.Localidades?.OrderBy(l => l.Nombre)
                       ?? Enumerable.Empty<LocationDto>();
            }
            catch (Exception ex)
            {
                // El checkout sigue usable (el usuario carga la localidad a mano), pero la falla
                // externa queda registrada para saber si el proveedor de datos está caído.
                _logger.LogWarning(ex, "API georef no devolvió localidades para {Provincia}", provinciaNombre);
                return Enumerable.Empty<LocationDto>();
            }
        }

        // Clases internas para mapear el JSON complejo de Georef
        private class GeorefResponse
        {
            public List<LocationDto> Provincias { get; set; }
            public List<LocationDto> Localidades { get; set; }
        }
    }
}
