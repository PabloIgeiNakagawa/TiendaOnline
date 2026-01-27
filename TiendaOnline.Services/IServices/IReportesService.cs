using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Services.IServices
{
    public interface IReportesService
    {
        Task<DashboardViewModel> ObtenerDashboardAsync(int periodo);
    }
}
