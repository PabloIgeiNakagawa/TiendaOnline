using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Logs()
        {
            var model = await _auditoriaService.ObtenerAuditoriasAsync();
            return View(model);
        }
    }
}
