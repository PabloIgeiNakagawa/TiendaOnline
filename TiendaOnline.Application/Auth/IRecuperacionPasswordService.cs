namespace TiendaOnline.Application.Auth
{
    /// <summary>
    /// Recuperación de contraseña: pedir un reset por email y aplicar la nueva
    /// contraseña con un token firmado por DataProtection (sin migración: el
    /// token no vive en la base).
    /// La respuesta del POST de pedido es genérica a propósito (anti-enumeración):
    /// el servicio no cuenta al caller si la cuenta existió o no.
    /// </summary>
    public interface IRecuperacionPasswordService
    {
        /// <summary>
        /// Si el email pertenece a una cuenta activa, genera el token (30 min)
        /// y envía el email con el enlace de reset. Caso contrario no envía nada
        /// y responde igual: decidir la respuesta es trabajo del controller.
        /// </summary>
        Task PedirResetPasswordAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Valida el token (firma, ventana de vida, cuenta activa) y devuelve el
        /// UsuarioId, o null si el enlace no sirve más (lo usa el GET del form).
        /// </summary>
        Task<int?> ValidarTokenResetAsync(string token, CancellationToken ct = default);

        /// <summary>
        /// Aplica la nueva contraseña al usuario del token. False = enlace
        /// inválido/vencido (nada cambió). El email de aviso sale después del
        /// cambio: si falla, la contraseña ya quedó actualizada igual.
        /// </summary>
        Task<bool> ResetPasswordAsync(string token, string nuevaPassword, CancellationToken ct = default);
    }
}
