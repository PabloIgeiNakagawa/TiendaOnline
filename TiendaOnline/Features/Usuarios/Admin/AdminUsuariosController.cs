using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Usuarios.Admin
{
    [Route("admin/usuarios")]
    [Authorize(Roles = "Administrador")]
    public class AdminUsuariosController : Controller
    {
        private readonly IUsuarioQueryService _usuarioQueryService;
        private readonly IUsuarioCommandService _usuarioCommandService;
        private readonly IRolQueryService _rolService;
        private readonly ILogger<AdminUsuariosController> _logger;

        public AdminUsuariosController(IUsuarioQueryService usuarioQueryService, IUsuarioCommandService usuarioCommandService, IRolQueryService rolService, ILogger<AdminUsuariosController> logger)
        {
            _usuarioQueryService = usuarioQueryService;
            _usuarioCommandService = usuarioCommandService;
            _rolService = rolService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(FiltrosListadoViewModel model)
        {
            var roles = await _rolService.ObtenerTodosAsync(HttpContext.RequestAborted);

            bool? estadoBool = model.Estado switch
            {
                "activo" => true,
                "inactivo" => false,
                _ => null
            };

            var pagedResult = await _usuarioQueryService.ObtenerUsuariosPaginadosAsync(model.Pagina, model.TamanoPagina, model.Busqueda, model.Rol, estadoBool, HttpContext.RequestAborted);

            var viewModel = new UsuarioListadoViewModel
            {
                UsuariosPaginados = pagedResult,
                Busqueda = model.Busqueda,
                Rol = model.Rol,
                Estado = estadoBool,
                RolesDisponibles = roles.Select(r => r.Nombre).ToList()
            };

            return View(viewModel);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _usuarioCommandService.DarAltaUsuarioAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Usuario {UsuarioId} re-activado por {Admin}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Usuario activado.");
            return RedirectToAction(nameof(Listado));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _usuarioCommandService.DarBajaUsuarioAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Usuario {UsuarioId} deshabilitado (baja) por {Admin}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Usuario dado de baja.");
            return RedirectToAction(nameof(Listado));
        }
    }
}
