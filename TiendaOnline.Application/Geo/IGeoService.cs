namespace TiendaOnline.Application.Geo
{
    public interface IGeoService
    {
        Task<IEnumerable<LocationDto>> GetProvinciasAsync();
        Task<IEnumerable<LocationDto>> GetLocalidadesAsync(string provinciaNombre);
    }
}
