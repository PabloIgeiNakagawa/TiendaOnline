namespace TiendaOnline.Application.Reportes
{
    public interface IReportesService
    {
        Task<DashboardDto> ObtenerDatosAsync(CancellationToken ct = default);
    }
}