using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Auditoria
{
    public interface IAuditoriaService
    {
        Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(ObtenerLogsRequest request);
        Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id);
    }
}
