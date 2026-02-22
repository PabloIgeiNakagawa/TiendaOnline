using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Logs(string busqueda, DateTime? fechaDesde, DateTime? fechaHasta, int pagina = 1, int tamanoPagina = 10)
        {
            ViewData["Title"] = "Auditoría del Sistema";

            if (pagina < 1) pagina = 1;
            if (tamanoPagina < 1) tamanoPagina = 10;

            var resultado = await _auditoriaService.ObtenerAuditoriasPaginadasAsync(pagina, tamanoPagina, busqueda, fechaDesde, fechaHasta);

            var model = new LogsViewModel
            {
                Paginacion = resultado,
                Busqueda = busqueda,
                TamanoPagina = tamanoPagina,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
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
