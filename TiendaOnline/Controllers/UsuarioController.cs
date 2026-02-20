using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Services.DTOs.Usuario;
using TiendaOnline.Services.IServices;
using TiendaOnline.ViewModels.Usuario;

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
            var usuario = await _usuarioService.ObtenerPerfil(id);
            if (usuario == null) return NotFound();
            var esPropioPerfil = User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString();
            ViewData["Title"] = esPropioPerfil ? "Mi Perfil" : "Perfil de Usuario";
            var model = new UsuarioPerfilViewModel
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Direccion = usuario.Direccion,
                FechaNacimiento = usuario.FechaNacimiento,
                FechaCreacion = usuario.FechaCreacion,
                Activo = usuario.Activo,
                UltimaFechaAlta = usuario.UltimaFechaAlta,
                UltimaFechaBaja = usuario.UltimaFechaBaja,
                esPropioPerfil = esPropioPerfil
            };
            return View(model);
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
        public async Task<IActionResult> EditarUsuario(UsuarioUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new UsuarioUpdateDto
            {
                UsuarioId = model.UsuarioId,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Telefono = model.Telefono
            };

            await _usuarioService.EditarUsuarioAsync(dto);
            TempData["MensajeExito"] = "El usuario se actualizó correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuario", new { dto.UsuarioId });
        }

    }
}
