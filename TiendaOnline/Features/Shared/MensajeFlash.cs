using TiendaOnline.Enums;

namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Mensaje flash que viaja en TempData y se renderiza como toast. Un solo mensaje
    /// por request (la última escritura gana: el flujo PRG setea uno y redirige).
    /// </summary>
    public class MensajeFlash
    {
        public TipoMensaje Tipo { get; set; }
        public string Texto { get; set; } = "";
    }
}
