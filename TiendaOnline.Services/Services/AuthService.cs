using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Services.DTOs.Account;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly TiendaContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public AuthService(TiendaContext context, IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<ClaimsPrincipal?> GenerarPrincipalAsync(string email, string password)
        {
            var usuarioDto = await ValidarCredencialesAsync(email, password);
            if (usuarioDto == null) return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioDto.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuarioDto.Nombre),
                new Claim(ClaimTypes.Email, usuarioDto.Email),
                new Claim(ClaimTypes.Role, usuarioDto.Rol)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            return new ClaimsPrincipal(identity);
        }
        
        public async Task Register(RegisterDto model)
        {
            model.Email = model.Email.Trim().ToLower();

            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
                throw new EmailDuplicadoException(model.Email);

            var nuevoUsuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaNacimiento = model.FechaNacimiento,
                Rol = (Rol)model.RolId,
                FechaCreacion = DateTime.Now
            };

            nuevoUsuario.Contrasena = _passwordHasher.HashPassword(nuevoUsuario, model.Contrasena);

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
        }

        private async Task<UsuarioDto?> ValidarCredencialesAsync(string email, string password)
        {
            var usuario = await ObtenerPorEmailAsync(email);

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

        private async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
