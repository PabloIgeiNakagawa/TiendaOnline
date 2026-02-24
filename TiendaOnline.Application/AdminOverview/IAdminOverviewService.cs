namespace TiendaOnline.Application.AdminOverview
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewDto> ObtenerResumenHomeAsync();
    }
}
