using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Common;
using TiendaOnline.Infrastructure.Persistence;
using EntidadAuditoria = TiendaOnline.Domain.Entities.Auditoria;

namespace TiendaOnline.Infrastructure.Services.Auditoria
{
    public class AuditoriaService : IAuditoriaService
    {
        // Techo del export: el CSV es una herramienta operativa, no un backup.
        // Sin tope, "exportar todo" en shared hosting puede agotar memoria.
        private const int TopeExportacion = 10_000;

        private readonly TiendaContext _context;

        public AuditoriaService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AuditoriaListaDto>> ObtenerAuditoriasPaginadasAsync(ObtenerLogsRequest request, CancellationToken ct = default)
        {
            var filtradas = AplicarFiltros(_context.Auditorias.AsNoTracking(), request);

            // El COUNT va sobre la query base, no sobre la proyección al DTO.
            var totalRegistros = await filtradas.CountAsync(ct);
            var items = await ProyectarLista(filtradas)
                .OrderByDescending(a => a.Fecha)
                .Skip((request.Pagina - 1) * request.TamanoPagina)
                .Take(request.TamanoPagina)
                .ToListAsync(ct);

            return new PagedResult<AuditoriaListaDto>(items, totalRegistros, request.Pagina, request.TamanoPagina);
        }

        public async Task<List<AuditoriaListaDto>> ObtenerAuditoriasParaExportarAsync(ObtenerLogsRequest request, CancellationToken ct = default)
        {
            return await ProyectarLista(AplicarFiltros(_context.Auditorias.AsNoTracking(), request))
                .OrderByDescending(a => a.Fecha)
                .Take(TopeExportacion)
                .ToListAsync(ct);
        }

        public async Task<AuditoriaDetalleDto?> ObtenerDetalleAuditoriaAsync(int id, CancellationToken ct = default)
        {
            return await _context.Auditorias
                .AsNoTracking()
                .Where(a => a.AuditoriaId == id)
                .Select(a => new AuditoriaDetalleDto
                {
                    Accion = a.Accion,
                    TablaAfectada = a.TablaAfectada,
                    DatosAnteriores = a.DatosAnteriores,
                    DatosNuevos = a.DatosNuevos,
                    EntidadId = a.EntidadId
                })
                .FirstOrDefaultAsync(ct);
        }

        private static IQueryable<EntidadAuditoria> AplicarFiltros(IQueryable<EntidadAuditoria> query, ObtenerLogsRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Busqueda))
            {
                var busqueda = request.Busqueda.Trim().ToLower();
                query = query.Where(a =>
                    (a.Usuario != null &&
                        (a.Usuario.Nombre.ToLower().Contains(busqueda) ||
                         a.Usuario.Apellido.ToLower().Contains(busqueda))) ||
                    a.TablaAfectada.ToLower().Contains(busqueda) ||
                    a.Accion.ToLower().Contains(busqueda) ||
                    a.EntidadId.ToLower().Contains(busqueda));
            }

            // Historial de una entidad puntual: tabla + id viajan juntos.
            if (!string.IsNullOrWhiteSpace(request.TablaAfectada))
                query = query.Where(a => a.TablaAfectada == request.TablaAfectada);

            if (!string.IsNullOrWhiteSpace(request.EntidadId))
                query = query.Where(a => a.EntidadId == request.EntidadId);

            if (request.FechaDesde.HasValue)
                query = query.Where(a => a.Fecha >= request.FechaDesde.Value);

            if (request.FechaHasta.HasValue)
            {
                var hastaFinal = request.FechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.Fecha <= hastaFinal);
            }

            return query;
        }

        // La proyección navega al usuario: EF resuelve el join solo y las filas
        // sin usuario (jobs/webhooks) se muestran como Sistema en el panel.
        private static IQueryable<AuditoriaListaDto> ProyectarLista(IQueryable<EntidadAuditoria> query)
        {
            return query.Select(a => new AuditoriaListaDto
            {
                AuditoriaId = a.AuditoriaId,
                Fecha = a.Fecha,
                UsuarioNombreCompleto = a.Usuario == null ? "Sistema" : $"{a.Usuario.Nombre} {a.Usuario.Apellido}",
                UsuarioEmail = a.Usuario == null ? null : a.Usuario.Email,
                Accion = a.Accion,
                TablaAfectada = a.TablaAfectada,
                EntidadId = a.EntidadId
            });
        }
    }
}
