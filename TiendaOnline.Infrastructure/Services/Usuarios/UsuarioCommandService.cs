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

        public async Task DarBajaUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId, ct);
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

            await _context.SaveChangesAsync(ct);
        }

        public async Task DarAltaUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId, ct);
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

            await _context.SaveChangesAsync(ct);
        }

        public async Task EditarUsuarioAsync(int usuarioId, UsuarioUpdateDto dto, CancellationToken ct = default)
        {
            // El destino lo firma el parámetro (id del route, validado por la política
            // EsDuenioDelPerfil): el UsuarioId dentro del dto queda ignorado — aun si un
            // POST trae el id de otro usuario, no edita perfiles ajenos (IDOR de la
            // auditoría 07/11).
            var usuario = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, ct);

            if (usuario == null) throw new KeyNotFoundException($"No se encontró el usuario con ID {usuarioId}");

            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Telefono = dto.Telefono;

            await _context.SaveChangesAsync(ct);
        }
    }
}
