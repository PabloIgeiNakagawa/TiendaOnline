using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Features.Usuarios
{
    [Authorize(Policy = "EsDuenioDelPerfil")]
    [Route("[controller]")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioQueryService _usuarioQueryService;
        private readonly IUsuarioCommandService _usuarioCommandService;

        public UsuariosController(IUsuarioQueryService usuarioQueryService, IUsuarioCommandService usuarioCommandService)
        {
            _usuarioQueryService = usuarioQueryService;
            _usuarioCommandService = usuarioCommandService;
        }

        [HttpGet("Perfil/{id}")]
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            var usuario = await _usuarioQueryService.ObtenerPerfil(id);
            if (usuario == null) return NotFound();
            var esPropioPerfil = User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString();
            
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

        [HttpGet("Editar/{id}")]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _usuarioQueryService.ObtenerUsuarioAsync(id);
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

        [HttpPost("Editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(int id, UsuarioUpdateViewModel model)
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

            await _usuarioCommandService.EditarUsuarioAsync(dto);
            TempData["MensajeExito"] = "Tu información se actualizó correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = dto.UsuarioId });
        }
    }
}
