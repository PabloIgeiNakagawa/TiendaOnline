using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Auth;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Enums;
using TiendaOnline.Features.Home;
using TiendaOnline.Features.Usuarios.Admin;

namespace TiendaOnline.Features.Accounts
{
    [Route("[controller]")]
    public class AccountsController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IRolQueryService _rolService;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IAuthService authService, IRolQueryService rolQueryService, ILogger<AccountsController> logger)
        {
            _authService = authService;
            _rolService = rolQueryService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel();

            if (User.IsInRole("Administrador"))
            {
                var roles = await _rolService.ObtenerTodosAsync(HttpContext.RequestAborted);
                model.RolesDisponibles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nombre
                }).ToList();
            }
            else
            {
                model.RolesDisponibles = new List<SelectListItem>();
                model.RolId = (int)Rol.Usuario;
            }

            return View(model);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("registro")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar roles si el modelo no es válido
                var roles = await _rolService.ObtenerTodosAsync(HttpContext.RequestAborted);
                model.RolesDisponibles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nombre
                }).ToList();
                return View(model);
            }

            var esAdmin = User.IsInRole("Administrador");

            if (!esAdmin)
            {
                model.RolId = (int)Rol.Usuario;
            }

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

            await _authService.RegisterAsync(dto, permitirRolPersonalizado: esAdmin, HttpContext.RequestAborted);
            _logger.LogInformation("Usuario registrado: {Email} (rol {RolId})", dto.Email, dto.RolId);

            if (esAdmin)    
            {
                TempData["MensajeExito"] = "¡Usuario registrado!";
                return RedirectToAction(nameof(AdminUsuariosController.Listado), "AdminUsuarios");
            }
            else
            {
                TempData["MensajeExito"] = "¡Te has registrado!";
                return RedirectToAction(nameof(Login));
            } 
        }

        [HttpGet("[action]")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuarioDto = await _authService.ValidarCredencialesAsync(model.Email, model.Contrasena, HttpContext.RequestAborted);

            if (usuarioDto == null)
            {
                // El email intentado es la señal para detectar fuerza bruta / credential stuffing
                // (ValidarCredenciales no distingue email inexistente de contraseña incorrecta).
                _logger.LogWarning("Login fallido para {Email}", model.Email);
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

            // Prevenir session fixation: limpiar sesión anterior antes de crear nueva
            await HttpContext.SignOutAsync("CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true
            });

            var role = principal.FindFirstValue(ClaimTypes.Role);

            return role == "Administrador"
                ? RedirectToAction(nameof(HomeController.IndexAdmin), "Home")
                : RedirectToAction(nameof(HomeController.Index), "Home");
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
                _logger.LogWarning("Login con Google falló durante la autenticación externa");
                TempData["MensajeError"] = "Hubo un error al continuar con Google.";
                return RedirectToAction(nameof(Login));
            }
                
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);  
                
            // Buscamos si ya existe en la DB
            var usuarioDto = await _authService.ObtenerUsuarioPorEmailAsync(email, HttpContext.RequestAborted);

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
                    RolId = (int)Rol.Usuario,
                    // Generamos una contraseña criptográficamente aleatoria
                    // El usuario nunca la va a usar, porque entrará con Google
                    Contrasena = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                };

                await _authService.RegisterAsync(nuevoUsuario, ct: HttpContext.RequestAborted);

                // Lo volvemos a buscar ahora que ya se guardó en la DB y tiene un UsuarioId asignado
                usuarioDto = await _authService.ObtenerUsuarioPorEmailAsync(email, HttpContext.RequestAborted);
            }

            // Defensa: si el re-fetch no devuelve nada (email vacío, carrera de datos),
            // cortamos acá con mensaje genérico en vez de tirar un NRE como 500.
            if (usuarioDto == null)
            {
                _logger.LogWarning("Login con Google: no se pudo resolver el usuario con email {Email}", email);
                TempData["MensajeError"] = "No pudimos iniciar sesión con Google. Intentá de nuevo.";
                return RedirectToAction(nameof(Login));
            }

            // Usuario deshabilitado por el panel: no puede iniciar sesión ni con Google
            // (AuthService ya lo bloquea en Password; acá cubrimos la ruta OAuth)
            if (!usuarioDto.Activo)
            {
                _logger.LogWarning("Login con Google bloqueado: cuenta desactivada {Email}", email);
                TempData["MensajeError"] = "Tu cuenta fue deshabilitada. Contactate con soporte.";
                return RedirectToAction(nameof(Login));
            }

            // Armamos los claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioDto.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuarioDto.Nombre),
                new Claim(ClaimTypes.Surname, usuarioDto.Apellido ?? ""),
                new Claim(ClaimTypes.MobilePhone, usuarioDto.Telefono ?? ""),
                new Claim(ClaimTypes.Email, usuarioDto.Email),
                new Claim(ClaimTypes.Role, usuarioDto.Rol)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Prevenir session fixation: limpiar sesión anterior antes de crear nueva
            await HttpContext.SignOutAsync("CookieAuth");
            // Iniciamos sesión y borramos la temporal
            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true
            });
            await HttpContext.SignOutAsync("ExternalCookie");

            return usuarioDto.Rol == "Administrador"
                ? RedirectToAction(nameof(HomeController.IndexAdmin), "Home")
                : RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction(nameof(Login));
        }
    }
}
