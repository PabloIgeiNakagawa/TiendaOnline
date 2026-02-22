namespace TiendaOnline.Features.Admin.Reportes
{
    public interface IReportesService
    {
        Task<DashboardDTO> ObtenerDatosAsync();
    }
}
