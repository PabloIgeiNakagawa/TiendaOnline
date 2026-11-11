using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Auditoria;
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
            var request = new ObtenerLogsRequest
            {
                Busqueda = filtro.Busqueda,
                FechaDesde = filtro.FechaDesde,
                FechaHasta = filtro.FechaHasta,
                Pagina = filtro.Pagina,
                TamanoPagina = filtro.TamanoPagina
            };

            var resultado = await _auditoriaService.ObtenerAuditoriasPaginadasAsync(request, HttpContext.RequestAborted);

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
            var detalle = await _auditoriaService.ObtenerDetalleAuditoriaAsync(id, HttpContext.RequestAborted);
            if (detalle == null) return NotFound();

            return Json(detalle);
        }
    }
}
