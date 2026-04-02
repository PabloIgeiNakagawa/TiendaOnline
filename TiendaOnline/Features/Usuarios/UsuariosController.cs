using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Features.Usuarios
{
    [Authorize(Policy = "EsDuenioDelPerfil")]
    [Route("[controller]")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioQueryService _usuarioQueryService;
        private readonly IUsuarioCommandService _usuarioCommandService;
        private readonly IDireccionService _direccionService;

        public UsuariosController(IUsuarioQueryService usuarioQueryService, IUsuarioCommandService usuarioCommandService, IDireccionService direccionService)
        {
            _usuarioQueryService = usuarioQueryService;
            _usuarioCommandService = usuarioCommandService;
            _direccionService = direccionService;
        }

        [HttpGet("Perfil/{id}")]
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            var usuario = await _usuarioQueryService.ObtenerPerfil(id);
            if (usuario == null) return NotFound();
            
            var direccionesDto = await _direccionService.ObtenerDireccionesAsync(id);
            var direcciones = direccionesDto.Select(d => new DireccionViewModel
            {
                DireccionId = d.DireccionId,
                Etiqueta = d.Etiqueta,
                Calle = d.Calle,
                Numero = d.Numero,
                Piso = d.Piso,
                Departamento = d.Departamento,
                Localidad = d.Localidad,
                Provincia = d.Provincia,
                CodigoPostal = d.CodigoPostal,
                Observaciones = d.Observaciones,
                EsPrincipal = d.EsPrincipal,
                Activo = d.Activo,
                UsuarioId = id
            }).ToList();

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
                CantidadPedidos = usuario.CantidadPedidos,
                TotalGastado = usuario.TotalGastado,
                Direcciones = direcciones
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

        [HttpGet("AgregarDireccion/{usuarioId}")]
        public IActionResult AgregarDireccion(int usuarioId)
        {
            var model = new DireccionViewModel { UsuarioId = usuarioId };
            return View("AgregarEditarDireccion", model);
        }

        [HttpPost("AgregarDireccion/{usuarioId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarDireccion(int usuarioId, DireccionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AgregarEditarDireccion", model);
            }

            var dto = new DireccionDto
            {
                Etiqueta = model.Etiqueta,
                Calle = model.Calle,
                Numero = model.Numero,
                Piso = model.Piso,
                Departamento = model.Departamento,
                Localidad = model.Localidad,
                Provincia = model.Provincia,
                CodigoPostal = model.CodigoPostal,
                Observaciones = model.Observaciones,
                EsPrincipal = model.EsPrincipal,
                Activo = true
            };

            await _direccionService.GuardarDireccionAsync(usuarioId, dto);
            TempData["MensajeExito"] = "Dirección agregada correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = usuarioId });
        }

        [HttpGet("EditarDireccion/{direccionId}")]
        public async Task<IActionResult> EditarDireccion(int direccionId)
        {
            var direccion = await _direccionService.ObtenerPorIdAsync(direccionId);
            if (direccion == null) return NotFound();

            var model = new DireccionViewModel
            {
                DireccionId = direccion.DireccionId,
                Etiqueta = direccion.Etiqueta,
                Calle = direccion.Calle,
                Numero = direccion.Numero,
                Piso = direccion.Piso,
                Departamento = direccion.Departamento,
                Localidad = direccion.Localidad,
                Provincia = direccion.Provincia,
                CodigoPostal = direccion.CodigoPostal,
                Observaciones = direccion.Observaciones,
                EsPrincipal = direccion.EsPrincipal,
                Activo = direccion.Activo,
                UsuarioId = direccion.UsuarioId
            };
            return View("AgregarEditarDireccion", model);
        }

        [HttpPost("EditarDireccion/{direccionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarDireccion(int direccionId, DireccionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AgregarEditarDireccion", model);
            }

            var dto = new DireccionDto
            {
                DireccionId = model.DireccionId,
                Etiqueta = model.Etiqueta,
                Calle = model.Calle,
                Numero = model.Numero,
                Piso = model.Piso,
                Departamento = model.Departamento,
                Localidad = model.Localidad,
                Provincia = model.Provincia,
                CodigoPostal = model.CodigoPostal,
                Observaciones = model.Observaciones,
                EsPrincipal = model.EsPrincipal,
                Activo = model.Activo
            };

            await _direccionService.ActualizarDireccionAsync(direccionId, dto);
            TempData["MensajeExito"] = "Dirección actualizada correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = model.UsuarioId });
        }

        [HttpPost("EliminarDireccion/{direccionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDireccion(int direccionId, int usuarioId)
        {
            var direccion = await _direccionService.ObtenerPorIdAsync(direccionId);
            if (direccion == null) return NotFound();

            await _direccionService.EliminarDireccionAsync(direccionId);
            TempData["MensajeExito"] = "Dirección eliminada correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = usuarioId });
        }
    }
}
