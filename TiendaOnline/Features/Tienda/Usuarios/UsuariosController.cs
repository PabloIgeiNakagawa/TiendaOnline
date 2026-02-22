using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Services.DTOs.Usuario;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Features.Tienda.Usuarios
{
    [Route("[controller]")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet("[action]")]
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

        [HttpGet("[action]")]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            ViewData["Title"] = "Editar Usuario";
            var usuario = await _usuarioService.ObtenerUsuarioAsync(id);
            if (usuario == null) return NotFound();
            var model = new UsuarioUpdateViewModel
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono
            };
            return View(model);
        }

        [HttpPost("[action]")]
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
            TempData["MensajeExito"] = "Tu información se actualizó correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = dto.UsuarioId });
        }
    }
}
