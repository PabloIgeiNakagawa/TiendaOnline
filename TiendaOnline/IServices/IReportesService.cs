using TiendaOnline.Models;

namespace TiendaOnline.IServices
{
    public interface IReportesService
    {
        Task<DashboardViewModel> ObtenerDashboardAsync(int periodo);
    }
}
