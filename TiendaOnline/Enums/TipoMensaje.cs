namespace TiendaOnline.Enums
{
    /// <summary>
    /// Tipo de mensaje flash que se muestra al usuario detrás de un redirect (patrón PRG).
    /// Maneja el estilo y comportamiento del toast en el parcial _MensajeTempData:
    /// Exito se auto-esconde; Error queda en pantalla hasta que cierra el usuario.
    /// </summary>
    public enum TipoMensaje
    {
        Exito = 0,
        Error = 1
    }
}
