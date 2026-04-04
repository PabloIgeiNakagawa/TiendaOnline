using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Usuarios
{
    public class UsuarioCommandService : IUsuarioCommandService
    {
        private readonly TiendaContext _context;

        public UsuarioCommandService(TiendaContext context)
        {
            _context = context;
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
