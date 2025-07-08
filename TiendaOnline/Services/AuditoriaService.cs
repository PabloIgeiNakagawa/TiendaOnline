using Microsoft.EntityFrameworkCore;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(TiendaContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<Auditoria>> ObtenerAuditoriasAsync()
        {
            return await _context.Auditorias
                .Include(a => a.Usuario)
                .ToListAsync();
        }
        public async Task RegistrarAuditoriaAsync(Auditoria auditoria)
        {
            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync();
        }

        public int ObtenerUsuarioId()
        {
            var usuarioIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("UsuarioId")?.Value;
            if (usuarioIdClaim != null && int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return usuarioId; // Retorna el UsuarioId como entero
            }
            return -1;
        }

    }
}
