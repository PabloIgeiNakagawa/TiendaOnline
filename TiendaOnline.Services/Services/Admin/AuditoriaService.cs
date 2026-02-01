using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.IServices.Admin;

namespace TiendaOnline.Services.Services.Admin
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly TiendaContext _context;

        public AuditoriaService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<List<Auditoria>> ObtenerAuditoriasAsync()
        {
            return await _context.Auditorias
                .Include(a => a.Usuario)
                .ToListAsync();
        }

    }
}
