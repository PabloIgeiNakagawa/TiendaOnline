using Microsoft.AspNetCore.Identity;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.DTOs.Account;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public AuthService(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        public async Task<UsuarioDto?> ValidarCredencialesAsync(string email, string password)
        {
            var usuario = await _usuarioService.ObtenerPorEmailAsync(email);

            if (usuario == null) return null;

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.Contrasena, password);

            if (resultado == PasswordVerificationResult.Failed) return null;

            return new UsuarioDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString()
            };
        }
    }
}
