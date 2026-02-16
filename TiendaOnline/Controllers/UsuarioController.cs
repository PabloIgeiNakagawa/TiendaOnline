using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            var usuario = await _usuarioService.ObtenerUsuarioAsync(id);
            return View(usuario);
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            ViewData["Title"] = "Editar Usuario";
            var usuario = await _usuarioService.ObtenerUsuarioAsync(id);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(int id, Usuario usuarioEditado)
        {
            await _usuarioService.EditarUsuarioAsync(id, usuarioEditado);
            TempData["MensajeExito"] = "El usuario se actualizó correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuario", new { id });
        }

    }
}
