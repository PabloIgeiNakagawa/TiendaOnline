using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Areas.Admin.ViewModels.Usuario;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Services.DTOs.Usuario;
using TiendaOnline.Services.IServices;
using TiendaOnline.ViewModels.Account;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Listado(int pagina = 1, int tamanoPagina = 10, string? busqueda = null, string? rol = null, string? estado = null)
        {
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

        [HttpGet]
        public IActionResult CrearUsuario()
        {
            ViewData["Title"] = "Crear Usuario";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(RegisterViewModel model)
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
                    Direccion = model.Direccion,
                    FechaNacimiento = model.FechaNacimiento,
                    Contrasena = model.Contrasena,
                    RolId = model.RolId
                };

                await _usuarioService.CrearUsuarioAsync(dto);
                TempData["MensajeExito"] = "Usuario creado correctamente.";
                return RedirectToAction("Usuario", "Listado", new { area = "Admin" });
            }
            catch (EmailDuplicadoException)
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _usuarioService.DarAltaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario activado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _usuarioService.DarBajaUsuarioAsync(id);
            TempData["MensajeExito"] = "Usuario dado de baja.";
            return RedirectToAction(nameof(Index));
        }
    }
}
