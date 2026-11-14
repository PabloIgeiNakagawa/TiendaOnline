namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Texto "hace X" de un tiempo transcurrido en pantalla: lo usa el reparto
    /// ("en camino hace 3 h") y las cards de Mis Pedidos del cliente.
    /// Más de 48 h ya no se entiende en horas ("hace 6814 h"): se cuenta en días.
    /// </summary>
    public static class TiempoTranscurrido
    {
        public static string Formatear(TimeSpan delta)
        {
            if (delta < TimeSpan.FromMinutes(1)) return "hace instantes";
            if (delta < TimeSpan.FromHours(1)) return $"hace {(int)delta.TotalMinutes} min";
            if (delta < TimeSpan.FromHours(48)) return $"hace {(int)delta.TotalHours} h";
            return $"hace {(int)delta.TotalDays} días";
        }
    }
}
