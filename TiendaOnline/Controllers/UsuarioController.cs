﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TiendaOnline.Exceptions;
using TiendaOnline.Models;
using TiendaOnline.Services;

namespace TiendaOnline.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // Registrar 
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Usuario usuario)
        {
            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            try
            {
                await _usuarioService.CrearUsuarioAsync(usuario);
                return RedirectToAction("Usuarios","Admin");
            }
            catch (EmailDuplicadoException ex)
            {
                ModelState.AddModelError("Email", ex.Message);
                return View(usuario);
            }
        }

        // Login y Logout
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _usuarioService.ObtenerPorEmailAsync(model.Email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "Credenciales inválidas");
                return View(model);
            }

            var passwordHasher = new PasswordHasher<Usuario>();
            var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.Contrasena, model.Contrasena);

            if (resultado == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Credenciales inválidas");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim("UsuarioId", usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString())
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login", "Usuario");
        }

        // Usuarios
        [HttpGet]
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            var usuario = await _usuarioService.ObtenerUsuarioAsync(id);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DarAltaUsuario(int id)
        {
            await _usuarioService.DarAltaUsuarioAsync(id);
            TempData["MensajeExito"] = "El usuario se dió de alta correctamente.";
            return RedirectToAction("Usuarios", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DarBajaUsuario(int id)
        {
            await _usuarioService.DarBajaUsuarioAsync(id);
            TempData["MensajeExito"] = "El usuario se dió de baja correctamente.";
            return RedirectToAction("Usuarios", "Admin");
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _usuarioService.ObtenerUsuarioAsync(id);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(int id, Usuario usuarioEditado)
        {
            await _usuarioService.EditarUsuarioAsync(id, usuarioEditado);
            TempData["MensajeExito"] = "El usuario se actualizó correctamente.";
            return RedirectToAction("PerfilUsuario", "Usuario", new { id });
        }

    }
}
