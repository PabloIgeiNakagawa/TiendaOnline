using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TiendaOnline.Infrastructure.Persistence
{
    /// <summary>
    /// JSON de las filas de auditoría: enums por nombre ("Enviado", no 2) para que
    /// el detalle se lea con los mismos labels que las columnas de estados.
    /// </summary>
    internal static class AuditoriaJson
    {
        private static readonly JsonSerializerSettings Ajustes = new()
        {
            Converters = { new StringEnumConverter() }
        };

        public static string Serializar(object? valor) =>
            JsonConvert.SerializeObject(valor, Ajustes);
    }
}
