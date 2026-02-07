using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Auditoria;

namespace TiendaOnline.Services.IServices.Admin
{
    public interface IAuditoriaService
    {
        Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(int pagina, int cantidad, string busqueda, DateTime? desde, DateTime? hasta);
        Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id);
    }
}
