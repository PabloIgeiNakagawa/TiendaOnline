using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Services.IServices
{
    public interface IAuditoriaService
    {
        Task<List<Auditoria>> ObtenerAuditoriasAsync();
    }
}
