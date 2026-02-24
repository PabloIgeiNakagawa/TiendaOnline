namespace TiendaOnline.Application.AdminOverview
{
    public class AdminOverviewDto
    {
        // Status del Sistema
        public bool DbOnline { get; set; }
        public string AppVersion { get; set; }
        public string Environment { get; set; }

        // Timeline de Auditoría (Cambios)
        public List<AuditoriaEntryDTO> UltimosCambios { get; set; } = new();

        // Pedidos Estancados
        public List<PedidoEstancadoDTO> PedidosEstancados { get; set; } = new();
    }

    public class AuditoriaEntryDTO
    {
        public string UsuarioNombre { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class PedidoEstancadoDTO
    {
        public int PedidoId { get; set; }
        public string ClienteNombre { get; set; }
        public DateTime Fecha { get; set; }
        public double HorasTranscurridas { get; set; }
    }
}
