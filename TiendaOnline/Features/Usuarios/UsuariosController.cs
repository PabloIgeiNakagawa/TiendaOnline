using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Usuarios
{
    [Authorize(Policy = "EsDuenioDelPerfil")]
    [Route("[controller]")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioQueryService _usuarioQueryService;
        private readonly IUsuarioCommandService _usuarioCommandService;
        private readonly IDireccionService _direccionService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(IUsuarioQueryService usuarioQueryService, IUsuarioCommandService usuarioCommandService, IDireccionService direccionService, ILogger<UsuariosController> logger)
        {
            _usuarioQueryService = usuarioQueryService;
            _usuarioCommandService = usuarioCommandService;
            _direccionService = direccionService;
            _logger = logger;
        }

        [HttpGet("Perfil/{id}")]
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            var usuario = await _usuarioQueryService.ObtenerPerfil(id, HttpContext.RequestAborted);
            if (usuario == null) return NotFound();

            var direccionesDto = await _direccionService.ObtenerDireccionesAsync(id, HttpContext.RequestAborted);
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
            var usuario = await _usuarioQueryService.ObtenerUsuarioAsync(id, HttpContext.RequestAborted);
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
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Telefono = model.Telefono
            };

            // El usuario a editar es el id del route (único valor que validó la política
            // EsDuenioDelPerfil): el hidden UsuarioId del form se ignora — un valor
            // que viaja en el POST nunca es autorización.
            await _usuarioCommandService.EditarUsuarioAsync(id, dto, HttpContext.RequestAborted);
            _logger.LogInformation("Perfil del usuario {UsuarioId} editado", id);
            TempData.SetMensajeExito("Tu información se actualizó correctamente.");
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id });
        }

        // ------------------------------------------------------------------
        // Cambio de contraseña desde el perfil
        // ------------------------------------------------------------------

        [HttpGet("CambiarPassword/{id}")]
        public async Task<IActionResult> CambiarPassword(int id)
        {
            var usuario = await _usuarioQueryService.ObtenerUsuarioAsync(id, HttpContext.RequestAborted);
            if (usuario == null) return NotFound();

            return View(new CambiarPasswordViewModel());
        }

        [HttpPost("CambiarPassword/{id}")]
        [ValidateAntiForgeryToken]
        // Fuerza bruta contra la contraseña actual: el form verifica el hash viejo
        // en cada envío, mismo motivo que el limiter del reset.
        [EnableRateLimiting("cambiar-password")]
        public async Task<IActionResult> CambiarPassword(int id, CambiarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // El usuario a cambiar es el id del route (único valor que validó la
            // política EsDuenioDelPerfil): el POST no firma nada por su cuenta.
            try
            {
                await _usuarioCommandService.CambiarPasswordAsync(id, model.ContrasenaActual, model.ContrasenaNueva, HttpContext.RequestAborted);
                _logger.LogInformation("Contraseña del usuario {UsuarioId} cambiada desde su perfil", id);
                TempData.SetMensajeExito("Tu contraseña se cambió correctamente.");
                return RedirectToAction("PerfilUsuario", "Usuarios", new { id });
            }
            catch (DomainException ex)
            {
                // Guard de dominio (contraseña actual incorrecta): mensaje curado del service.
                _logger.LogWarning(ex, "No se pudo cambiar la contraseña del usuario {UsuarioId}: {Motivo}", id, ex.Message);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo el cambio de contraseña del usuario {UsuarioId}", id);
                ModelState.AddModelError(string.Empty, "No pudimos cambiar la contraseña. Intentá de nuevo en unos minutos.");
                return View(model);
            }
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

            await _direccionService.GuardarDireccionAsync(usuarioId, dto, HttpContext.RequestAborted);
            TempData.SetMensajeExito("Dirección agregada correctamente.");
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = usuarioId });
        }

        [HttpGet("EditarDireccion/{direccionId}")]
        public async Task<IActionResult> EditarDireccion(int direccionId)
        {
            var direccion = await _direccionService.ObtenerPorIdAsync(direccionId, HttpContext.RequestAborted);
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

            await _direccionService.ActualizarDireccionAsync(direccionId, dto, HttpContext.RequestAborted);
            TempData.SetMensajeExito("Dirección actualizada correctamente.");
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = model.UsuarioId });
        }

        [HttpPost("EliminarDireccion/{direccionId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDireccion(int direccionId, int usuarioId)
        {
            var direccion = await _direccionService.ObtenerPorIdAsync(direccionId, HttpContext.RequestAborted);
            if (direccion == null) return NotFound();

            await _direccionService.EliminarDireccionAsync(direccionId, HttpContext.RequestAborted);
            TempData.SetMensajeExito("Dirección eliminada correctamente.");
            return RedirectToAction("PerfilUsuario", "Usuarios", new { id = usuarioId });
        }
    }
}
