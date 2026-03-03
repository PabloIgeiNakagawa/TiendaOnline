using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Application.Auth;
using TiendaOnline.Domain.Exceptions;

namespace TiendaOnline.Features.Accounts
{
    [Route("[controller]")]
    public class AccountsController : Controller
    {
        private readonly IAuthService _authService;

        public AccountsController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("[action]")]
        public IActionResult Register()
        {
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
                    RolId = model.RolId
                };

                await _authService.RegisterAsync(dto);
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
            return View();
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuarioDto = await _authService.ValidarCredencialesAsync(model.Email, model.Contrasena);

            if (usuarioDto == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectas.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioDto.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuarioDto.Nombre),
                new Claim(ClaimTypes.Email, usuarioDto.Email),
                new Claim(ClaimTypes.Role, usuarioDto.Rol)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

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
