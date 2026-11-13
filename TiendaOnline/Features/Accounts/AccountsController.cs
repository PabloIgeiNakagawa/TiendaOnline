using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Auth;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Enums;
using TiendaOnline.Features.Home;
using TiendaOnline.Features.Repartidor;
using TiendaOnline.Features.Usuarios.Admin;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Accounts
{
    [Route("[controller]")]
    public class AccountsController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IRecuperacionPasswordService _recuperacionService;
        private readonly IRolQueryService _rolService;
        private readonly ICarritoSesionMigrador _migradorCarrito;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IAuthService authService, IRolQueryService rolQueryService, ILogger<AccountsController> logger, ICarritoSesionMigrador migradorCarrito, IRecuperacionPasswordService recuperacionService)
        {
            _authService = authService;
            _rolService = rolQueryService;
            _logger = logger;
            _migradorCarrito = migradorCarrito;
            _recuperacionService = recuperacionService;
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
                TempData.SetMensajeExito("¡Usuario registrado!");
                return RedirectToAction(nameof(AdminUsuariosController.Listado), "AdminUsuarios");
            }
            else
            {
                TempData.SetMensajeExito("¡Te has registrado!");
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
                // Un usuario puede no tener teléfono (lo de Google, o el reset de
                // contraseña de una cuenta sin teléfono): el claim con null explota.
                new Claim(ClaimTypes.MobilePhone, usuarioDto.Telefono ?? ""),
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

            await FusionarCarritoDeSesionAsync(usuarioDto.UsuarioId);

            var role = principal.FindFirstValue(ClaimTypes.Role);

            if (role == "Administrador")
                return RedirectToAction(nameof(HomeController.IndexAdmin), "Home");

            // El personal de reparto va a su cola: la tienda es el destino del cliente.
            if (role == "Repartidor")
                return RedirectToAction(nameof(RepartidorController.Index), "Repartidor");

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // ------------------------------------------------------------------
        // Recuperación de contraseña
        // ------------------------------------------------------------------

        [HttpGet("[action]")]
        public IActionResult OlvideMiPassword()
        {
            return View(new OlvideMiPasswordViewModel());
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("olvide-password")]
        public async Task<IActionResult> OlvideMiPassword(OlvideMiPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _recuperacionService.PedirResetPasswordAsync(model.Email, HttpContext.RequestAborted);

            // Respuesta genérica SIEMPRE (anti-enumeración): si el email no existe
            // el service no envió nada y el usuario ve el mismo mensaje que si
            // existiera — nunca se revela qué cuentas están registradas.
            TempData.SetMensajeExito("Si el email está registrado, te enviamos un enlace para recuperar tu contraseña. Revisá también la carpeta de spam.");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ResetPassword(string? token)
        {
            var valido = await _recuperacionService.ValidarTokenResetAsync(token ?? "", HttpContext.RequestAborted);
            if (valido is null)
            {
                // Enlace vencido, alterado o ya usado: salida amable al flujo para pedir otro.
                TempData.SetMensajeError("El enlace no es válido o ya expiró. Pedí uno nuevo.");
                return RedirectToAction(nameof(OlvideMiPassword));
            }

            return View(new ResetPasswordViewModel { Token = token! });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("olvide-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var cambio = await _recuperacionService.ResetPasswordAsync(model.Token, model.Contrasena, HttpContext.RequestAborted);
            if (!cambio)
            {
                // El enlace murió mientras completaba el formulario (venció o alguien
                // ya lo usó): mensaje amable con salida al flujo de pedir otro.
                TempData.SetMensajeError("El enlace no es válido o ya expiró. Pedí uno nuevo.");
                return RedirectToAction(nameof(OlvideMiPassword));
            }

            TempData.SetMensajeExito("¡Listo! Tu contraseña fue actualizada. Iniciá sesión con la nueva.");
            return RedirectToAction(nameof(Login));
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
                TempData.SetMensajeError("Hubo un error al continuar con Google.");
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
                TempData.SetMensajeError("No pudimos iniciar sesión con Google. Intentá de nuevo.");
                return RedirectToAction(nameof(Login));
            }

            // Usuario deshabilitado por el panel: no puede iniciar sesión ni con Google
            // (AuthService ya lo bloquea en Password; acá cubrimos la ruta OAuth)
            if (!usuarioDto.Activo)
            {
                _logger.LogWarning("Login con Google bloqueado: cuenta desactivada {Email}", email);
                TempData.SetMensajeError("Tu cuenta fue deshabilitada. Contactate con soporte.");
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

            await FusionarCarritoDeSesionAsync(usuarioDto.UsuarioId);

            if (usuarioDto.Rol == "Administrador")
                return RedirectToAction(nameof(HomeController.IndexAdmin), "Home");

            // El personal de reparto va a su cola: la tienda es el destino del cliente.
            if (usuarioDto.Rol == "Repartidor")
                return RedirectToAction(nameof(RepartidorController.Index), "Repartidor");

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// El carrito que el anónimo venía armando en la sesión pasa a su carrito
        /// guardado en BD. Un fallo acá no puede romper el login: si algo sale mal
        /// solo queda el carrito persistido que ya tenía la cuenta.
        /// </summary>
        private async Task FusionarCarritoDeSesionAsync(int usuarioId)
        {
            try
            {
                if (await _migradorCarrito.FusionarConCarritoGuardadoAsync(usuarioId))
                {
                    TempData.SetMensajeExito("¡Bienvenido! Sumamos al carrito guardado los productos que venías armando.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo fusionar el carrito de sesión para el usuario {UsuarioId}", usuarioId);
            }
        }
    }
}
