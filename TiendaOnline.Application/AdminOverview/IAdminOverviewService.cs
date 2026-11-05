namespace TiendaOnline.Application.AdminOverview
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewDto> ObtenerResumenHomeAsync(CancellationToken ct = default);
        Task<PedidosEstancadosPaginadoDto> ObtenerPedidosEstancadosPaginadoAsync(int pagina, int tamanoPagina = 5, CancellationToken ct = default);
    }
}
