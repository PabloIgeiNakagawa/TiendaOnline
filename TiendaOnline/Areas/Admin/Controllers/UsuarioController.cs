using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _usuarioService.ObtenerUsuariosAsync();
            return View(usuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _usuarioService.DarAltaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario activado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _usuarioService.DarBajaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario dado de baja.";
            return RedirectToAction(nameof(Index));
        }
    }
}
