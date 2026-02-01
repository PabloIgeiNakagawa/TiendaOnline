using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Services.IServices.Admin
{
    public interface IAuditoriaService
    {
        Task<List<Auditoria>> ObtenerAuditoriasAsync();
    }
}
