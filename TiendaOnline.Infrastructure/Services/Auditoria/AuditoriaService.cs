using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Common;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Auditoria
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly TiendaContext _context;

        public AuditoriaService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(ObtenerLogsRequest request, CancellationToken ct = default)
        {
            var query = _context.Auditorias
                .AsNoTracking()
                .Include(a => a.Usuario)
                .AsQueryable();

            // Filtros de búsqueda
            if (!string.IsNullOrWhiteSpace(request.Busqueda))
            {
                var busqueda = request.Busqueda.Trim().ToLower();
                query = query.Where(a =>
                    (a.Usuario != null &&
                        (a.Usuario.Nombre.ToLower().Contains(busqueda) ||
                         a.Usuario.Apellido.ToLower().Contains(busqueda))) ||
                    a.TablaAfectada.ToLower().Contains(busqueda) ||
                    a.Accion.ToLower().Contains(busqueda));
            }

            // Filtro por rango de fechas
            if (request.FechaDesde.HasValue)
                query = query.Where(a => a.Fecha >= request.FechaDesde.Value);

            if (request.FechaHasta.HasValue)
            {
                var hastaFinal = request.FechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Fecha <= hastaFinal);
            }

            // Conteo total
            var totalRegistros = await query.CountAsync(ct);

            // Proyección al DTO
            var items = await query
                .OrderByDescending(a => a.Fecha)
                .Skip((request.Pagina - 1) * request.TamanoPagina)
                .Take(request.TamanoPagina)
                .Select(a => new AuditoriaListaDto
                {
                    AuditoriaId = a.AuditoriaId,
                    Fecha = a.Fecha,
                    // Las filas de sistema no tienen usuario asociado: se muestran como Sistema en el panel.
                    UsuarioNombreCompleto = a.Usuario == null ? "Sistema" : $"{a.Usuario.Nombre} {a.Usuario.Apellido}",
                    UsuarioEmail = a.Usuario == null ? null : a.Usuario.Email,
                    Accion = a.Accion,
                    TablaAfectada = a.TablaAfectada
                })
                .ToListAsync(ct);

            return new PagedResult<AuditoriaListaDto>(items, totalRegistros, request.Pagina, request.TamanoPagina);
        }

        public async Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id, CancellationToken ct = default)
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
                .FirstOrDefaultAsync(ct);
        }

    }
}
