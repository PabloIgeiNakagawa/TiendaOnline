using TiendaOnline.Domain.DTOs;

namespace TiendaOnline.Services.IServices
{
    public interface IReportesService
    {
        Task<DashboardDTO> ObtenerDatosAsync();
    }
}
