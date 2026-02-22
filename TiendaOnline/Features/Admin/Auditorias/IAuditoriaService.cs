using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Admin.Auditorias
{
    public interface IAuditoriaService
    {
        Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(int pagina, int cantidad, string busqueda, DateTime? desde, DateTime? hasta);
        Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id);
    }
}
