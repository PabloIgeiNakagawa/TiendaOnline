using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Features.Shared.Models;
using TiendaOnline.Features.Tienda.Usuarios;

namespace TiendaOnline.Features.Admin.Usuarios
{
    public class UsuariosAdminService : IUsuariosAdminService
    {
        private readonly TiendaContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosAdminService(TiendaContext context, IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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

        public async Task CrearUsuarioAsync(UsuarioCreateDto usuarioDto)
        {
            usuarioDto.Email = usuarioDto.Email.Trim().ToLower();

            if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioDto.Email))
                throw new EmailDuplicadoException(usuarioDto.Email);

            var nuevoUsuario = new Usuario
            {
                Nombre = usuarioDto.Nombre,
                Apellido = usuarioDto.Apellido,
                Email = usuarioDto.Email,
                Telefono = usuarioDto.Telefono,
                FechaNacimiento = usuarioDto.FechaNacimiento,
                Rol = (Rol)usuarioDto.RolId,
                FechaCreacion = DateTime.Now
            };

            nuevoUsuario.Contrasena = _passwordHasher.HashPassword(nuevoUsuario, usuarioDto.Contrasena);

            _context.Usuarios.Add(nuevoUsuario);
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

        public async Task EditarUsuarioAsync(UsuarioUpdateDto dto)
        {
            var usuario = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.UsuarioId == dto.UsuarioId);

            if (usuario == null) throw new KeyNotFoundException($"No se encontró el usuario con ID {dto.UsuarioId}");

            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Telefono = dto.Telefono;

            await _context.SaveChangesAsync();
        }
    }
}
