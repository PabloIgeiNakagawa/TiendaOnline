namespace TiendaOnline.Application.AdminOverview
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewDto> ObtenerResumenHomeAsync();
        Task<PedidosEstancadosPaginadoDto> ObtenerPedidosEstancadosPaginadoAsync(int pagina, int tamanoPagina = 5);
    }
}
