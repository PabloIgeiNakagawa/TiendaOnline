namespace TiendaOnline.Application.Reportes
{
    public interface IReportesService
    {
        Task<DashboardDTO> ObtenerDatosAsync();
    }
}