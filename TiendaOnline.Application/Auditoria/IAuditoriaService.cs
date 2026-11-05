using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Auditoria
{
    public interface IAuditoriaService
    {
        Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(ObtenerLogsRequest request, CancellationToken ct = default);
        Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id, CancellationToken ct = default);
    }
}
