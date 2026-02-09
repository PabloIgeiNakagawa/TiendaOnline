using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Auditoria;
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

        public async Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(int pagina, int cantidad, string? busqueda = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var query = _context.Auditorias
                .AsNoTracking()
                .Include(a => a.Usuario)
                .AsQueryable();

            // Filtros de búsqueda
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim().ToLower();
                query = query.Where(a =>
                    a.Usuario.Nombre.ToLower().Contains(busqueda) ||
                    a.Usuario.Apellido.ToLower().Contains(busqueda) ||
                    a.TablaAfectada.ToLower().Contains(busqueda) ||
                    a.Accion.ToLower().Contains(busqueda));
            }

            // Filtro por rango de fechas
            if (fechaDesde.HasValue)
                query = query.Where(a => a.Fecha >= fechaDesde.Value);

            if (fechaHasta.HasValue)
            {
                var hastaFinal = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Fecha <= hastaFinal);
            }

            // Conteo total
            var totalRegistros = await query.CountAsync();

            // Proyección al DTO
            var items = await query
                .OrderByDescending(a => a.Fecha)
                .Skip((pagina - 1) * cantidad)
                .Take(cantidad)
                .Select(a => new AuditoriaListaDto
                {
                    AuditoriaId = a.AuditoriaId,
                    Fecha = a.Fecha,
                    UsuarioNombreCompleto = $"{a.Usuario.Nombre} {a.Usuario.Apellido}",
                    UsuarioEmail = a.Usuario.Email,
                    Accion = a.Accion,
                    TablaAfectada = a.TablaAfectada
                })
                .ToListAsync();

            return new PagedResult<AuditoriaListaDto>(items, totalRegistros, pagina, cantidad);
        }

        public async Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id)
        {
            return await _context.Auditorias
                .AsNoTracking()
                .Where(a => a.AuditoriaId == id)
                .Select(a => new AuditoriaDetalleDto
                {
                    DatosAnteriores = a.DatosAnteriores,
                    DatosNuevos = a.DatosNuevos,
                    EntidadId = a.EntidadId
                })
                .FirstOrDefaultAsync();
        }

    }
}
