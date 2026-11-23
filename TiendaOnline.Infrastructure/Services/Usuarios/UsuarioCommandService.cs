using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
namespace TiendaOnline.Infrastructure.Services.Usuarios
{
    public class UsuarioCommandService : IUsuarioCommandService
    {
        private readonly TiendaContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IAuditoriaWriter _auditoriaWriter;
        private readonly ILogger<UsuarioCommandService> _logger;

        public UsuarioCommandService(TiendaContext context, IPasswordHasher<Usuario> passwordHasher, IEmailService emailService, IAuditoriaWriter auditoriaWriter, ILogger<UsuarioCommandService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _auditoriaWriter = auditoriaWriter;
            _logger = logger;
        }

        public async Task DarBajaUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId, ct);
            if (usuario == null) throw new UsuarioNoEncontradoException("El usuario indicado no existe.");

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
            if (usuario == null) throw new UsuarioNoEncontradoException("El usuario indicado no existe.");

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
            // EsDuenioDelPerfil): el UsuarioId dentro del dto queda ignorado — aun si
            // el POST trae el id de otro usuario, no edita perfiles ajenos.
            var usuario = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, ct);

            if (usuario == null) throw new UsuarioNoEncontradoException("El usuario indicado no existe.");

            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Telefono = dto.Telefono;

            await _context.SaveChangesAsync(ct);
        }

        public async Task CambiarPasswordAsync(int usuarioId, string contrasenaActual, string contrasenaNueva, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, ct);
            if (usuario == null) throw new UsuarioNoEncontradoException("El usuario indicado no existe.");

            // La actual se verifica contra el hash guardado: sin este check, quien
            // deje la sesión abierta podría robar la cuenta a su dueño.
            var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.Contrasena, contrasenaActual);
            if (verificacion == PasswordVerificationResult.Failed)
                throw new PasswordActualIncorrectaException("La contraseña actual no es correcta.");

            // SuccessRehashNeeded: el hash viejo quedó débil (formato anterior) — se
            // rehashea con la nueva igual que en el caso feliz, así que el uso cae en
            // el mismo HashPassword de siempre.
            // El hash y su evento de auditoría comparten transacción: el cambio de
            // credencial no puede quedar sin registro.
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                usuario.Contrasena = _passwordHasher.HashPassword(usuario, contrasenaNueva);
                await _context.SaveChangesAsync(ct);

                // El hash está excluido del JSON de auditoría: se registra el evento
                // explícito sin datos para que el panel no muestre un update vacío.
                _auditoriaWriter.Registrar(
                    "Cambió contraseña",
                    nameof(Usuario),
                    usuarioId.ToString(),
                    new { },
                    new { });

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
                throw;
            }

            // El cambio ya está en la base: el aviso es best-effort (regla de la
            // casa — el email va después del commit, un fallo no deshace nada).
            try
            {
                await _emailService.EnviarEmailPasswordCambiadaAsync(usuario.Email, usuario.Nombre);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Cambio de contraseña aplicado pero el email de aviso no salió (usuario {UsuarioId})", usuarioId);
            }
        }
    }
}
