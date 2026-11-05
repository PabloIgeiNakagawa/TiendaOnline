namespace TiendaOnline.Application.Geo
{
    public interface IGeoService
    {
        // HttpClient real: el ct cancela de verdad la HTTP request a la API georef.
        Task<IEnumerable<LocationDto>> GetProvinciasAsync(CancellationToken ct = default);
        Task<IEnumerable<LocationDto>> GetLocalidadesAsync(string provinciaNombre, CancellationToken ct = default);
    }
}
