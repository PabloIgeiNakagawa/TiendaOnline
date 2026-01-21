using TiendaOnline.Data;
using TiendaOnline.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Exceptions;
using TiendaOnline.IServices;
using Newtonsoft.Json;

namespace TiendaOnline.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly TiendaContext _context;
        private readonly PasswordHasher<Usuario> _passwordHasher;
        private readonly IAuditoriaService _auditoriaService;

        public UsuarioService(TiendaContext context, PasswordHasher<Usuario> passwordHasher, IAuditoriaService auditoriaService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditoriaService = auditoriaService;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<Usuario>> ObtenerUsuariosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<Usuario?> ObtenerUsuarioAsync(int usuarioId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
        }

        public async Task CrearUsuarioAsync(Usuario usuario)
        {
            usuario.Email = usuario.Email.Trim().ToLower();

            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                throw new EmailDuplicadoException(usuario.Email);

            usuario.Contrasena = _passwordHasher.HashPassword(usuario, usuario.Contrasena);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task DarBajaUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) throw new Exception("Usuario no encontrado.");

            var estadoAnterior = new
            {
                usuario.UsuarioId,
                usuario.Email,
                usuario.Activo,
                usuario.UltimaFechaBaja
            };

            usuario.Activo = false;
            usuario.UltimaFechaBaja = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task DarAltaUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) throw new Exception("Usuario no encontrado.");

            var estadoAnterior = new
            {
                usuario.UsuarioId,
                usuario.Email,
                usuario.Activo,
                usuario.UltimaFechaAlta
            };

            usuario.Activo = true;
            usuario.UltimaFechaAlta = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task EditarUsuarioAsync(int usuarioId, Usuario usuarioEditado)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) throw new Exception("Usuario no encontrado.");

            // Solo guardamos lo que realmente puede cambiar para no ensuciar el JSON de auditoría
            var datosAnteriores = new
            {
                usuario.Nombre,
                usuario.Apellido,
                usuario.Telefono,
                usuario.Direccion
            };

            usuario.Nombre = usuarioEditado.Nombre;
            usuario.Apellido = usuarioEditado.Apellido;
            usuario.Telefono = usuarioEditado.Telefono;
            usuario.Direccion = usuarioEditado.Direccion;

            await _context.SaveChangesAsync();
        }
    }
}
