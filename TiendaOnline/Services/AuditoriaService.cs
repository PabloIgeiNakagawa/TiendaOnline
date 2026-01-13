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
        public async Task RegistrarAccionAsync(string accion, object? datosAnteriores, object? datosNuevos)
        {
            var auditoria = new Auditoria
            {
                UsuarioId = ObtenerUsuarioId(),
                Accion = accion,
                DatosAnteriores = JsonConvert.SerializeObject(datosAnteriores ?? new { }),
                DatosNuevos = JsonConvert.SerializeObject(datosNuevos ?? new { }),
                Fecha = DateTime.Now
            };
            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync();
        }

        private int ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("UsuarioId")?.Value;

            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int id))
            {
                throw new UnauthorizedAccessException("No se pudo identificar al usuario activo.");
            }

            return id;
        }

    }
}
