using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
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
                new Claim(ClaimTypes.Surname, usuarioDto.Apellido),
                new Claim(ClaimTypes.MobilePhone, usuarioDto.Telefono),
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

            TempData["MensajeExito"] = $"¡Bienvenido, {usuarioDto.Nombre}!";

            return role == "Administrador"
                ? RedirectToAction("IndexAdmin", "Home")
                : RedirectToAction("Index", "Home");
        }

        [HttpGet("[action]")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (!result.Succeeded)
            {
                TempData["MensajeError"] = "Hubo un error al continuar con Google.";
                return RedirectToAction("Login");
            }
                
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);  
                
            // Buscamos si ya existe en la DB
            var usuarioDto = await _authService.ObtenerUsuarioPorEmailAsync(email);

            // SI NO EXISTE: Lo registramos automáticamente
            if (usuarioDto == null)
            {
                var nombre = result.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Usuario";
                var apellido = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

                var nuevoUsuario = new RegisterDto
                {
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    RolId = 0, // RolId 0 = Usuario
                    // Generamos una contraseña compleja aleatoria, ya que la DB requiere una
                    // El usuario nunca la va a usar, porque entrará con Google
                    Contrasena = Guid.NewGuid().ToString() + "aA1!"
                };

                await _authService.RegisterAsync(nuevoUsuario);

                // Lo volvemos a buscar ahora que ya se guardó en la DB y tiene un UsuarioId asignado
                usuarioDto = await _authService.ObtenerUsuarioPorEmailAsync(email);
            }

            // Armamos los claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioDto!.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuarioDto.Nombre),
                new Claim(ClaimTypes.Surname, usuarioDto.Apellido ?? ""),
                new Claim(ClaimTypes.MobilePhone, usuarioDto.Telefono ?? ""),
                new Claim(ClaimTypes.Email, usuarioDto.Email),
                new Claim(ClaimTypes.Role, usuarioDto.Rol)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Iniciamos sesión y borramos la temporal
            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
            await HttpContext.SignOutAsync("ExternalCookie");

            TempData["MensajeExito"] = "¡Has iniciado sesión con Google!";

            return usuarioDto.Rol == "Administrador"
                ? RedirectToAction("IndexAdmin", "Home")
                : RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}
