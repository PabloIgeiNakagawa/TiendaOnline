using System.Text.Json;
using TiendaOnline.Application.Carritos;

namespace TiendaOnline.Infrastructure.Services.Carritos
{
    /// <summary>
    /// Snapshot JSON del carrito: el formato es el mismo con el que la session lo
    /// guarda (serialización default de System.Text.Json sobre List&lt;ItemCarrito&gt;),
    /// así session y BD guardan exactamente el mismo shape.
    /// </summary>
    internal static class CarritoSnapshotJson
    {
        public static string Serializar(List<ItemCarrito> items)
            => JsonSerializer.Serialize(items);

        public static List<ItemCarrito> Deserializar(string? json)
            => json is null
                ? new List<ItemCarrito>()
                : JsonSerializer.Deserialize<List<ItemCarrito>>(json) ?? new List<ItemCarrito>();
    }
}
