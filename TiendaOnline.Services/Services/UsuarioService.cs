using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Services.IServices;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Usuario;

namespace TiendaOnline.Services.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly TiendaContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuarioService(TiendaContext context, IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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

        public async Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo)
        {
            var query = _context.Usuarios.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(u => u.Nombre.Contains(buscar) || u.Apellido.Contains(buscar) || u.Email.Contains(buscar));

            if (!string.IsNullOrEmpty(rol) && Enum.TryParse<Rol>(rol, out var rolEnum))
            {
                query = query.Where(u => u.Rol == rolEnum);
            }

            if (activo.HasValue)
                query = query.Where(u => u.Activo == activo.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Apellido)
                .Skip((pagina - 1) * cantidad)
                .Take(cantidad)
                .Select(u => new UsuarioListadoDto
                {
                    UsuarioId = u.UsuarioId,
                    NombreCompleto = $"{u.Nombre} {u.Apellido}",
                    Email = u.Email,
                    RolNombre = u.Rol.ToString(),
                    Activo = u.Activo
                })
                .ToListAsync();

            return new PagedResult<UsuarioListadoDto>(items, total, pagina, cantidad);
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
