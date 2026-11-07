using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TiendaOnline.Enums;

namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Único punto de escritura de mensajes flash en TempData. Los controllers y el
    /// DomainExceptionFilter comparten la misma API (la extension vive sobre
    /// ITempDataDictionary); el render queda en el parcial _MensajeTempData. El
    /// MensajeFlash viaja serializado a JSON string: el payload es primitivo y el
    /// provider (cookie o session) lo maneja igual.
    /// </summary>
    public static class TempDataExtensions
    {
        private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

        public static void SetMensajeExito(this ITempDataDictionary tempData, string mensaje)
            => SetMensaje(tempData, TipoMensaje.Exito, mensaje);

        public static void SetMensajeError(this ITempDataDictionary tempData, string mensaje)
            => SetMensaje(tempData, TipoMensaje.Error, mensaje);

        // Indexer = marca para borrar al leer (semántica flash del provider, como siempre).
        public static MensajeFlash? GetMensaje(this ITempDataDictionary tempData)
        {
            var json = tempData[TempDataKeys.FlashMessage] as string;
            return json is null ? null : JsonSerializer.Deserialize<MensajeFlash>(json, OpcionesJson);
        }

        private static void SetMensaje(this ITempDataDictionary tempData, TipoMensaje tipo, string mensaje)
            => tempData[TempDataKeys.FlashMessage] = JsonSerializer.Serialize(
                new MensajeFlash { Tipo = tipo, Texto = mensaje ?? "" }, OpcionesJson);
    }
}
