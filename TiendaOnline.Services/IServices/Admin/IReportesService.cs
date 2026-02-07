using TiendaOnline.Services.DTOs.Admin.Reportes;

namespace TiendaOnline.Services.IServices.Admin
{
    public interface IReportesService
    {
        Task<DashboardDTO> ObtenerDatosAsync();
    }
}
