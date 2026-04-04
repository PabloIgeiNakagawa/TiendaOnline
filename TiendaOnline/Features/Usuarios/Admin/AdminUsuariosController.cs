using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Features.Usuarios.Admin
{
    [Route("admin/usuarios")]
    [Authorize(Roles = "Administrador")]
    public class AdminUsuariosController : Controller
    {
        private readonly IUsuarioQueryService _usuarioQueryService;
        private readonly IUsuarioCommandService _usuarioCommandService;
        private readonly IRolQueryService _rolService;

        public AdminUsuariosController(IUsuarioQueryService usuarioQueryService, IUsuarioCommandService usuarioCommandService, IRolQueryService rolService)
        {
            _usuarioQueryService = usuarioQueryService;
            _usuarioCommandService = usuarioCommandService;
            _rolService = rolService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(FiltrosListadoViewModel model)
        {
            var roles = await _rolService.ObtenerTodosAsync();

            bool? estadoBool = model.Estado switch
            {
                "activo" => true,
                "inactivo" => false,
                _ => null
            };

            var pagedResult = await _usuarioQueryService.ObtenerUsuariosPaginadosAsync(model.Pagina, model.TamanoPagina, model.Busqueda, model.Rol, estadoBool);

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
            await _usuarioCommandService.DarAltaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario activado.";
            return RedirectToAction(nameof(Listado));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _usuarioCommandService.DarBajaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario dado de baja.";
            return RedirectToAction(nameof(Listado));
        }
    }
}
