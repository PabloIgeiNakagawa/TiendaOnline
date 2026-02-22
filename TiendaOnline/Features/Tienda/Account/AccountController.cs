using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;

namespace TiendaOnline.Features.Tienda.Account
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("[action]")]
        public IActionResult Register()
        {
            ViewData["Title"] = "Registrarse";
            return View();
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var dto = new RegisterDto
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    FechaNacimiento = model.FechaNacimiento,
                    Contrasena = model.Contrasena,
                    RolId = (int)Rol.Usuario
                };

                await _authService.Register(dto);
                TempData["MensajeExito"] = "¡Te has registrado!";
                return RedirectToAction("Login");
            }
            catch (EmailDuplicadoException ex)
            {
                ModelState.AddModelError("Email", ex.Message);
                return View(model);
            }
        }

        [HttpGet("[action]")]
        public IActionResult Login()
        {
            ViewData["Title"] = "Iniciar Sesión";
            return View();
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var principal = await _authService.GenerarPrincipalAsync(model.Email, model.Contrasena);

            if (principal == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                return View(model);
            }

            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            var role = principal.FindFirstValue(ClaimTypes.Role);
            return role == "Administrador"
                ? RedirectToAction("Index", "HomeAdmin")
                : RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}
