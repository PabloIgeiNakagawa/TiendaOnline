using TiendaOnline.Application.Pedidos.Queries;

namespace TiendaOnline.Features.Repartidor
{
    /// <summary>
    /// La cola del reparto de un vistazo: los entregables arriba y los entregados
    /// de las últimas 24 horas abajo, en la sección plegable del historial.
    /// </summary>
    public class RepartidorViewModel
    {
        public List<RepartidorFilaViewModel> Pendientes { get; set; } = new();
        public List<RepartidorFilaViewModel> EntregadosUltimoDia { get; set; } = new();

        // Resumen del día arriba del todo: entregas concretadas desde la 00:00
        // (el historial plegable cubre las últimas 24 h, no solo hoy).
        public int EntregadasHoy { get; set; }
    }

    /// <summary>
    /// Fila de la cola con lo que el reparto usa en la calle: contacto del cliente
    /// (WhatsApp), ruta con GPS del teléfono (Google Maps sin origin: la app pide
    /// la ubicación al abrirla) e items para revisar lo que lleva.
    /// </summary>
    public class RepartidorFilaViewModel
    {
        public int PedidoId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public List<PedidoProductoListadoDto> Productos { get; set; } = new();

        /// <summary>Dirección completa de envío; null = el pedido es retiro en local.</summary>
        public string? DireccionCompleta { get; set; }

        public bool EsRetiroLocal => DireccionCompleta == null;

        /// <summary>Texto que muestra la columna "Entregar en" en ambos casos.</summary>
        public string DireccionDisplay => DireccionCompleta ?? "Retiro en local";

        /// <summary>
        /// Ruta al destino armada por Google Maps con la ubicación actual del
        /// teléfono (sin origin en la URL: Maps la pide al abrir el link).
        /// </summary>
        public string? LinkComoLlegar => DireccionCompleta == null
            ? null
            : $"https://www.google.com/maps/dir/?api=1&destination={Uri.EscapeDataString(DireccionCompleta)}";

        /// <summary>Chat directo con el cliente para coordinar la entrega (solo envíos).</summary>
        public string? LinkWhatsapp
        {
            get
            {
                if (DireccionCompleta == null || string.IsNullOrWhiteSpace(Telefono)) return null;

                var numero = new string(Telefono.Where(char.IsDigit).ToArray());
                return numero.Length == 0 ? null : $"https://wa.me/{numero}";
            }
        }

        /// <summary>Texto "hace X" desde que el pedido salió de la sucursal.</summary>
        public string? HaceEnCamino => FechaEnvio == null ? null : FormatearTiempo(DateTime.Now - FechaEnvio.Value);

        /// <summary>En camino hace más de 24 h: algo se atascó, la fila se resalta.</summary>
        public bool EstaColgada => FechaEnvio != null && DateTime.Now - FechaEnvio.Value > TimeSpan.FromHours(24);

        private static string FormatearTiempo(TimeSpan delta)
        {
            if (delta < TimeSpan.FromMinutes(1)) return "hace instantes";
            if (delta < TimeSpan.FromHours(1)) return $"hace {(int)delta.TotalMinutes} min";
            // Más de 48 h ya no se entiende en horas ("hace 6814 h"): se cuenta en días.
            if (delta < TimeSpan.FromHours(48)) return $"hace {(int)delta.TotalHours} h";
            return $"hace {(int)delta.TotalDays} días";
        }
    }
}
