using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Areas.Admin.ViewModels.Auditoria;
using TiendaOnline.Services.IServices.Admin;


namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles= "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly IAuditoriaService _auditoriaService;

        public AuditoriaController(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        public async Task<IActionResult> Logs(string busqueda, DateTime? fechaDesde, DateTime? fechaHasta, int pagina = 1, int tamanoPagina = 10)
        {
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

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var detalle = await _auditoriaService.ObtenerDetalleAuditoriaAsync(id);
            if (detalle == null) return NotFound();

            return Json(detalle);
        }
    }
}
