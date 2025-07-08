using TiendaOnline.Models;

namespace TiendaOnline.IServices
{
    public interface IAuditoriaService
    {
        Task<List<Auditoria>> ObtenerAuditoriasAsync();
        int ObtenerUsuarioId();
        Task RegistrarAuditoriaAsync(Auditoria auditoria);
    }
}
