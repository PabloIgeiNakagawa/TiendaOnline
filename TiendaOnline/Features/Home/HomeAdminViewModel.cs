using TiendaOnline.Enums;
using TiendaOnline.Extensions;

namespace TiendaOnline.Features.Home
{
    public class HomeAdminViewModel
    {
        // --- SECCIÓN: STATUS DEL SISTEMA ---
        public bool EstaDbOnline { get; set; }
        public string VersionApp { get; set; }
        public string Entorno { get; set; }

        // --- SECCIÓN: RESUMEN DIARIO ---
        public ResumenDiarioViewModel ResumenDiario { get; set; }

        // --- SECCIÓN: TIMELINE DE AUDITORÍA ---
        // Usamos una lista de una clase interna para que sea fácil de iterar
        public List<AuditoriaItemViewModel> UltimosCambios { get; set; } = new();

        // --- SECCIÓN: ALERTAS DE PEDIDOS ---
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; }
        public int TotalRegistros { get; set; }

        // --- SECCIÓN: MOVIMIENTOS DE STOCK ---
        public List<MovimientoStockItemViewModel> UltimosMovimientosStock { get; set; } = new();

        // --- SECCIÓN: PRODUCTOS BAJO STOCK ---
        public List<ProductoBajoStockItemViewModel> ProductosBajoStock { get; set; } = new();

        // --- SECCIÓN: PEDIDOS RECIENTES ---
        public List<PedidoRecienteItemViewModel> PedidosRecientes { get; set; } = new();
    }

    public class ResumenDiarioViewModel
    {
        public decimal VentasHoy { get; set; }
        public double PorcentajeVentas { get; set; }

        public int PedidosHoy { get; set; }
        public double PorcentajePedidos { get; set; }

        public int EnviadosHoy { get; set; }
        public double PorcentajeEnviados { get; set; }

        public int StockBajo { get; set; }

        public string FormatoMoneda(decimal valor) => "$" + valor.ToString("N0");
        public string FormatoPorcentaje(double valor) => valor >= 0 ? $"+{valor:F1}%" : $"{valor:F1}%";
        public string ClasePorcentaje(double valor) => valor >= 0 ? "text-success" : "text-danger";
    }

    public class AuditoriaItemViewModel
    {
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }

        // Helper para mostrar "hace 5 minutos" en la vista si querés
        public string FechaRelativa => CalcularFechaRelativa(Fecha);

        private static string CalcularFechaRelativa(DateTime fecha)
        {
            var ts = new TimeSpan(DateTime.Now.Ticks - fecha.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 60) return "ahora mismo";
            if (delta < 3600) return $"hace {ts.Minutes}m";
            if (delta < 86400) return $"hace {ts.Hours}h";
            return fecha.ToString("dd/MM HH:mm");
        }
    }

    public class PedidoEstancadoViewModel
    {
        public int PedidoId { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public double HorasTranscurridas{ get; set; }

        // Para definir el color de la alerta en Bootstrap dinámicamente
        public string ClaseColor => HorasTranscurridas > 72 ? "danger" : "warning";
    }

    public class MovimientoStockItemViewModel
    {
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public int TipoMovimientoId { get; set; }
        public TipoMovimiento TipoMovimiento => (TipoMovimiento)TipoMovimientoId;
        public DateTime Fecha { get; set; }
        public string Observaciones { get; set; }

        public string FechaRelativa => CalcularFechaRelativa(Fecha);

        private static string CalcularFechaRelativa(DateTime fecha)
        {
            var ts = new TimeSpan(DateTime.Now.Ticks - fecha.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 60) return "ahora mismo";
            if (delta < 3600) return $"hace {ts.Minutes}m";
            if (delta < 86400) return $"hace {ts.Hours}h";
            return fecha.ToString("dd/MM HH:mm");
        }
    }

    public class ProductoBajoStockItemViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string ImagenUrl { get; set; }
        public string Categoria { get; set; }
        public int Stock { get; set; }

        public string BadgeClase => Stock == 0 ? "bg-danger" : (Stock <= 3 ? "bg-danger" : "bg-warning");
        public string BadgeTexto => Stock == 0 ? "Agotado" : $"{Stock} unidades";
    }

    public class PedidoRecienteItemViewModel
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Cliente { get; set; }
        public decimal Total { get; set; }
        public string EstadoPedido { get; set; }
        public int EstadoPedidoId { get; set; }

        public string EstadoBadge => EstadoPedido switch
        {
            "Nuevo" => "bg-secondary",
            "EnPreparacion" => "bg-info",
            "Enviado" => "bg-primary",
            "Entregado" => "bg-success",
            "Cancelado" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
