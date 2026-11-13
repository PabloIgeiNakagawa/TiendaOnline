using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Auth;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Auth
{
    /// <summary>
    /// Token de reset sin migración: el payload viaja firmado por DataProtection
    /// (purpose propio, keys persistidas en App_Data) con userId + expiración +
    /// huella del hash vigente. La huella mata el enlace apenas cambia la
    /// contraseña: un token no es reutilizable después de un reset exitoso
    /// (aunque su ventana de 30 minutos no haya vencido).
    /// </summary>
    public class RecuperacionPasswordService : IRecuperacionPasswordService
    {
        private const string PropositoDelToken = "TiendaOnline.ResetPassword.v1";
        private static readonly TimeSpan VidaDefaultDelToken = TimeSpan.FromMinutes(30);

        private readonly TiendaContext _context;
        private readonly IDataProtector _protector;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly ILogger<RecuperacionPasswordService> _logger;
        private readonly TimeSpan _vidaDelToken;

        public RecuperacionPasswordService(
            TiendaContext context,
            IDataProtectionProvider dataProtectionProvider,
            IPasswordHasher<Usuario> passwordHasher,
            IEmailService emailService,
            ILogger<RecuperacionPasswordService> logger,
            TimeSpan? vidaDelToken = null)
        {
            _context = context;
            _protector = dataProtectionProvider.CreateProtector(PropositoDelToken);
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _logger = logger;
            _vidaDelToken = vidaDelToken ?? VidaDefaultDelToken;
        }

        public async Task PedirResetPasswordAsync(string email, CancellationToken ct = default)
        {
            var emailLimpio = email.Trim().ToLower();
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == emailLimpio && u.Activo, ct);

            // Sin cuenta activa no se envía nada: el controller responde el mismo
            // mensaje genérico que con cuenta existente (anti-enumeración).
            if (usuario is null)
            {
                _logger.LogInformation("Pedido de reset de contraseña para email sin cuenta activa");
                return;
            }

            var token = _protector.Protect(ArmarPayload(usuario, DateTimeOffset.UtcNow.Add(_vidaDelToken)));

            // Best-effort: si el envío falla el caller responde igual (el error
            // ruidoso delataría que la cuenta existe). El usuario reintenta.
            try
            {
                await _emailService.EnviarEmailResetPasswordAsync(usuario.Email, usuario.Nombre, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el email de reset de contraseña");
            }
        }

        public async Task<int?> ValidarTokenResetAsync(string token, CancellationToken ct = default)
        {
            var partes = DesarmarPayload(token);
            if (partes is null) return null;

            var (expiraUtcTicks, usuarioId, huella) = partes.Value;

            if (DateTimeOffset.UtcNow.UtcTicks >= expiraUtcTicks) return null;

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId && u.Activo, ct);
            if (usuario is null) return null;

            // Token firmado con la contraseña de otro momento: murió (reset previo
            // o cambio desde la cuenta) aunque la ventana no haya expirado.
            if (!string.Equals(huella, HuellaDelHash(usuario.Contrasena), StringComparison.Ordinal))
                return null;

            return usuarioId;
        }

        public async Task<bool> ResetPasswordAsync(string token, string nuevaPassword, CancellationToken ct = default)
        {
            var usuarioId = await ValidarTokenResetAsync(token, ct);
            if (usuarioId is null) return false;

            var usuario = await _context.Usuarios.FirstAsync(u => u.UsuarioId == usuarioId, ct);
            usuario.Contrasena = _passwordHasher.HashPassword(usuario, nuevaPassword);
            await _context.SaveChangesAsync(ct);

            // El cambio ya está en la base: el aviso es best-effort (regla de la
            // casa — el email va después del commit, un fallo no deshace nada).
            try
            {
                await _emailService.EnviarEmailPasswordCambiadaAsync(usuario.Email, usuario.Nombre);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Reset de contraseña aplicado pero el email de aviso no salió (usuario {UsuarioId})", usuarioId);
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Payload: "{expiración utc ticks}:{usuarioId}:{huella del hash vigente}"
        // ------------------------------------------------------------------

        private (long ExpiraUtcTicks, int UsuarioId, string Huella)? DesarmarPayload(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            string payload;
            try
            {
                // Firma, claves y formato corren por cuenta de DataProtection:
                // tamper, token de otro propósito o de otra app explotan acá.
                payload = _protector.Unprotect(token);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                _logger.LogWarning("Token de reset de contraseña inválido (firma/formato)");
                return null;
            }

            var partes = payload.Split(':');
            if (partes.Length != 3) return null;
            if (!long.TryParse(partes[0], out var expiraUtcTicks)) return null;
            if (!int.TryParse(partes[1], out var usuarioId)) return null;

            return (expiraUtcTicks, usuarioId, partes[2]);
        }

        private static string ArmarPayload(Usuario usuario, DateTimeOffset expira)
            => $"{expira.UtcTicks}:{usuario.UsuarioId}:{HuellaDelHash(usuario.Contrasena)}";

        /// <summary>
        /// 8 bytes del SHA-256 del hash guardado (16 hex): alcanza para detectar
        /// que la contraseña cambió sin meter el hash completo dentro del token.
        /// </summary>
        private static string HuellaDelHash(string hashContrasena)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashContrasena)), 0, 8);
    }
}
