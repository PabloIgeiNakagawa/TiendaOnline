using System.Net.Http.Json;
using TiendaOnline.Application.Geo;

namespace TiendaOnline.Infrastructure.Services.Geo
{
    public class GeoService : IGeoService
    {
        private readonly HttpClient _httpClient;

        public GeoService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<LocationDto>> GetProvinciasAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<GeorefResponse>("https://apis.datos.gob.ar/georef/api/provincias?campos=id,nombre");
            return response?.Provincias.OrderBy(p => p.Nombre) ?? Enumerable.Empty<LocationDto>();
        }

        public async Task<IEnumerable<LocationDto>> GetLocalidadesAsync(string provinciaNombre)
        {
            if (string.IsNullOrWhiteSpace(provinciaNombre))
                return Enumerable.Empty<LocationDto>();

            var provinciaEscapada = Uri.EscapeDataString(provinciaNombre);

            var url = $"https://apis.datos.gob.ar/georef/api/localidades?provincia={provinciaEscapada}&max=1000&campos=id,nombre";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<GeorefResponse>(url);

                return response?.Localidades?.OrderBy(l => l.Nombre)
                       ?? Enumerable.Empty<LocationDto>();
            }
            catch (Exception ex)
            {
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
