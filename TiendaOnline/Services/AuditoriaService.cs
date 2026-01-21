using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TiendaOnline.Data;
using TiendaOnline.Exceptions;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Services
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
