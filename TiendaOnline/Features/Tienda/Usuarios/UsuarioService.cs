using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Features.Tienda.Usuarios
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

        public async Task<UsuarioPerfilDto> ObtenerPerfil(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Direcciones)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario == null) throw new Exception("Usuario no encontrado.");

            var direccionPrincipal = usuario.Direcciones.FirstOrDefault(d => d.EsPrincipal);
            return new UsuarioPerfilDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Direccion = direccionPrincipal != null ? $"{direccionPrincipal.Calle} {direccionPrincipal.Numero}, {direccionPrincipal.Localidad}" : "Sin dirección",
                FechaNacimiento = usuario.FechaNacimiento,
                FechaCreacion = usuario.FechaCreacion,
                Activo = usuario.Activo,
                UltimaFechaAlta = usuario.UltimaFechaAlta,
                UltimaFechaBaja = usuario.UltimaFechaBaja
            };
        }

        public async Task<UsuarioUpdateDto> ObtenerUsuarioParaEdicionAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Direcciones)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario == null) throw new Exception("Usuario no encontrado.");

            var direccionPrincipal = usuario.Direcciones.FirstOrDefault(d => d.EsPrincipal);
            return new UsuarioUpdateDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono
            };
        }

        public async Task<Usuario?> ObtenerUsuarioAsync(int usuarioId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
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
