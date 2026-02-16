using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Services.DTOs.Usuario;
using TiendaOnline.Services.IServices;
using TiendaOnline.ViewModels.Account;

namespace TiendaOnline.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IAuthService _authService;

        public AccountController(IUsuarioService usuarioService, IAuthService authService)
        {
            _usuarioService = usuarioService;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "Registrarse";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
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
                    RolId = (int)Rol.Usuario
                };

                await _usuarioService.CrearUsuarioAsync(dto);

                return RedirectToAction("Login");
            }
            catch (EmailDuplicadoException)
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "Iniciar Sesión";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Llamamos a la lógica de negocio pura
            var usuarioDto = await _authService.ValidarCredencialesAsync(model.Email, model.Contrasena);

            if (usuarioDto == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                return View(model);
            }

            // Crear la identidad
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioDto.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuarioDto.Nombre),
                new Claim(ClaimTypes.Email, usuarioDto.Email),
                new Claim(ClaimTypes.Role, usuarioDto.Rol)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Persistencia física: La Cookie
            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            // Redirección basada en lógica de UI
            if (usuarioDto.Rol == "Administrador")
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}
