using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Auth;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Auth
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

        public async Task<UsuarioDto?> ValidarCredencialesAsync(string email, string password, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo, ct);
            if (usuario == null) return null;

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.Contrasena, password);
            if (resultado == PasswordVerificationResult.Failed) return null;

            return new UsuarioDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString(),
                Activo = usuario.Activo
            };
        }

        public async Task RegisterAsync(RegisterDto model, bool permitirRolPersonalizado = false, CancellationToken ct = default)
        {
            model.Email = model.Email.Trim().ToLower();

            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email, ct))
                throw new EmailDuplicadoException(model.Email);

            var nuevoUsuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                FechaNacimiento = model.FechaNacimiento,
                // Seguridad: el rol del cliente SOLO se respeta si el caller (controller admin)
                // lo pidió explícito con permitirRolPersonalizado. El resto cae a Usuario.
                Rol = permitirRolPersonalizado ? (Rol)model.RolId : Rol.Usuario,
                FechaCreacion = DateTime.Now
            };

            nuevoUsuario.Contrasena = _passwordHasher.HashPassword(nuevoUsuario, model.Contrasena);

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorEmailAsync(string email, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (usuario == null) return null;

            return new UsuarioDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString(),
                Activo = usuario.Activo
            };
        }

    }
}
