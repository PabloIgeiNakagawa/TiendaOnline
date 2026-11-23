using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.Auditorias
{
    [Route("admin/auditorias")]
    [Authorize(Roles = "Administrador")]
    public class AdminAuditoriasController : AdminControllerBase
    {
        private readonly IAuditoriaService _auditoriaService;

        public AdminAuditoriasController(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Logs(LogsFiltroViewModel filtro)
        {
            var request = AFiltro(filtro);
            var resultado = await _auditoriaService.ObtenerAuditoriasPaginadasAsync(request, HttpContext.RequestAborted);

            var model = new LogsViewModel
            {
                Paginacion = resultado,
                Busqueda = request.Busqueda,
                TablaAfectada = request.TablaAfectada,
                EntidadId = request.EntidadId,
                TamanoPagina = request.TamanoPagina,
                Pagina = request.Pagina,
                FechaDesde = request.FechaDesde,
                FechaHasta = request.FechaHasta
            };

            return View(model);
        }

        // Export CSV del historial filtrado: mismos parámetros del form (los viaja
        // el mismo GET vía formaction del botón), sin paginación. BOM UTF-8 y
        // separador ";" para que Excel lo abra directo.
        [HttpGet("[action]")]
        public async Task<IActionResult> Exportar(LogsFiltroViewModel filtro)
        {
            var filas = await _auditoriaService.ObtenerAuditoriasParaExportarAsync(AFiltro(filtro), HttpContext.RequestAborted);

            var sb = new StringBuilder();
            sb.AppendLine("Fecha;Usuario;Email;Acción;Entidad;ID");

            foreach (var fila in filas)
            {
                sb.AppendLine(string.Join(';',
                    Csv.Celda(fila.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                    Csv.Celda(fila.UsuarioNombreCompleto),
                    Csv.Celda(fila.UsuarioEmail ?? ""),
                    Csv.Celda(fila.Accion),
                    Csv.Celda(fila.TablaAfectada),
                    Csv.Celda(fila.EntidadId)));
            }

            // BOM UTF-8: sin él Excel adivina ANSI y los acentos salen rotos.
            return File(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString()),
                "text/csv",
                $"auditorias-{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var detalle = await _auditoriaService.ObtenerDetalleAuditoriaAsync(id, HttpContext.RequestAborted);
            if (detalle == null) return NotFound();

            return Json(detalle);
        }

        private static ObtenerLogsRequest AFiltro(LogsFiltroViewModel filtro) => new()
        {
            Busqueda = filtro.Busqueda,
            TablaAfectada = filtro.TablaAfectada,
            EntidadId = filtro.EntidadId,
            FechaDesde = filtro.FechaDesde,
            FechaHasta = filtro.FechaHasta,
            Pagina = filtro.Pagina,
            TamanoPagina = filtro.TamanoPagina
        };
    }
}
