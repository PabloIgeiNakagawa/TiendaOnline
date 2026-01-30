using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly IReportesService _reportesService;

        public ReportesController(IReportesService reportesService)
        {
            _reportesService = reportesService;
        }
        
        public async Task<IActionResult> Dashboard()
        {
            var model = await _reportesService.ObtenerDashboardAsync(0);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DatosDashboardJson(int periodo)
        {
            var model = await _reportesService.ObtenerDashboardAsync(periodo);
            return Json(model);
        }
    }
}
