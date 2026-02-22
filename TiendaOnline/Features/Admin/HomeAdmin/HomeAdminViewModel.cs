namespace TiendaOnline.Features.Admin.HomeAdmin
{
    public class HomeAdminViewModel
    {
        // --- SECCIÓN: STATUS DEL SISTEMA ---
        public bool EstaDbOnline { get; set; }
        public string VersionApp { get; set; }
        public string Entorno { get; set; }

        // --- SECCIÓN: TIMELINE DE AUDITORÍA ---
        // Usamos una lista de una clase interna para que sea fácil de iterar
        public List<AuditoriaItemViewModel> UltimosCambios { get; set; } = new();

        // --- SECCIÓN: ALERTAS DE PEDIDOS ---
        public List<PedidoEstancadoViewModel> PedidosEstancados { get; set; } = new();

        // --- SECCIÓN: ACCESOS RÁPIDOS (Opcional, pero ayuda a la vista) ---
        public int ConteoPendientes => PedidosEstancados.Count;
    }

    public class AuditoriaItemViewModel
    {
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }

        // Helper para mostrar "hace 5 minutos" en la vista si querés
        public string FechaRelativa => CalcularFechaRelativa(Fecha);

        private string CalcularFechaRelativa(DateTime fecha)
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
}
