using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Features.Usuarios.Admin
{
    [Route("Admin/[controller]")]
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
        public async Task<IActionResult> Listado(int pagina = 1, int tamanoPagina = 10, string? busqueda = null, string? rol = null, string? estado = null)
        {
            bool? estadoBool = estado switch
            {
                "activo" => true,
                "inactivo" => false,
                _ => null
            };

            var pagedResult = await _usuarioQueryService.ObtenerUsuariosPaginadosAsync(pagina, tamanoPagina, busqueda, rol, estadoBool);

            var viewModel = new UsuarioListadoViewModel
            {
                UsuariosPaginados = pagedResult,
                Busqueda = busqueda,
                RolSeleccionado = rol,
                EstadoSeleccionado = estadoBool
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> CrearUsuarioAsync()
        {
            var roles = await _rolService.ObtenerTodosAsync();
            var model = new CrearUsuarioViewModel();

            model.RolesDisponibles = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nombre
            }).ToList();

            return View(model);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar roles si el modelo no es válido
                var roles = await _rolService.ObtenerTodosAsync();
                model.RolesDisponibles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nombre
                }).ToList();
                return View(model);
            }

            var dto = new UsuarioCreateDto
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaNacimiento = model.FechaNacimiento,
                Contrasena = model.Contrasena,
                RolId = model.RolId
            };

            await _usuarioCommandService.CrearUsuarioAsync(dto);
            TempData["MensajeExito"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Listado));
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
