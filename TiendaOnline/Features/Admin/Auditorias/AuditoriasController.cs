using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Auditoria;

namespace TiendaOnline.Features.Admin.Auditorias
{
    [Route("Admin/[controller]")]
    [Authorize(Roles= "Administrador")]
    public class AuditoriasController : Controller
    {
        private readonly IAuditoriaService _auditoriaService;

        public AuditoriasController(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Logs(LogsFiltroViewModel filtro)
        {
            ViewData["Title"] = "Auditoría del Sistema";

            var request = new ObtenerLogsRequest
            {
                Busqueda = filtro.Busqueda,
                FechaDesde = filtro.FechaDesde,
                FechaHasta = filtro.FechaHasta,
                Pagina = filtro.Pagina,
                TamanoPagina = filtro.TamanoPagina
            };

            var resultado = await _auditoriaService.ObtenerAuditoriasPaginadasAsync(request);

            var model = new LogsViewModel
            {
                Paginacion = resultado,
                Busqueda = request.Busqueda,
                TamanoPagina = request.TamanoPagina,
                Pagina = request.Pagina,
                FechaDesde = request.FechaDesde,
                FechaHasta = request.FechaHasta
            };

            return View(model);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var detalle = await _auditoriaService.ObtenerDetalleAuditoriaAsync(id);
            if (detalle == null) return NotFound();

            return Json(detalle);
        }
    }
}
