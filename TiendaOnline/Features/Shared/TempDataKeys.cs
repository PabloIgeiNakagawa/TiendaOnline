namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Únicos keys válidos de TempData en la app. Un nuevo caso se suma acá como constante:
    /// un literal escrito a mano compila y falla en silencio (mensaje que nadie lee).
    /// El test de shape de ReglaDeMensajesFlash guarda esta regla.
    /// </summary>
    public static class TempDataKeys
    {
        /// <summary>Mensaje flash (MensajeFlash) — escrito por TempDataExtensions y leído por el parcial.</summary>
        public const string FlashMessage = "FlashMessage";

        /// <summary>Roundtrip del ViewModel del checkout entre ProcesarCheckout y Confirmacion (JSON).</summary>
        public const string CheckOutTemp = "CheckOutTemp";
    }
}
