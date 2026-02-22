using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Features.Tienda.Usuarios;

namespace TiendaOnline.Features.Admin.Usuarios
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(int pagina = 1, int tamanoPagina = 10, string? busqueda = null, string? rol = null, string? estado = null)
        {
            ViewData["Title"] = "Gestión de Usuarios";

            bool? estadoBool = estado switch
            {
                "activo" => true,
                "inactivo" => false,
                _ => null
            };

            var pagedResult = await _usuarioService.ObtenerUsuariosPaginadosAsync(pagina, tamanoPagina, busqueda, rol, estadoBool);

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
        public IActionResult CrearUsuario()
        {
            ViewData["Title"] = "Crear Usuario";
            return View();
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(UsuarioCreateDto model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
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

                await _usuarioService.CrearUsuarioAsync(dto);
                TempData["MensajeExito"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Listado));
            }
            catch (EmailDuplicadoException ex)
            {
                ModelState.AddModelError("Email", ex.Message);
                return View(model);
            }
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _usuarioService.DarAltaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario activado.";
            return RedirectToAction(nameof(Listado));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _usuarioService.DarBajaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario dado de baja.";
            return RedirectToAction(nameof(Listado));
        }
    }
}
