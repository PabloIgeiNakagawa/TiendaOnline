namespace TiendaOnline.Application.AdminOverview
{
    public class AdminOverviewDto
    {
        // Status del Sistema
        public bool DbOnline { get; set; }
        public string AppVersion { get; set; }

        // Resumen Diario
        public ResumenDiarioDto ResumenDiario { get; set; }

        // Timeline de Auditoría (Cambios)
        public List<AuditoriaEntryDto> UltimosCambios { get; set; } = new();

        // Pedidos Estancados
        public List<PedidoEstancadoDto> PedidosEstancados { get; set; } = new();

        // Movimientos de Stock Recientes
        public List<MovimientoStockDto> UltimosMovimientosStock { get; set; } = new();

        // Productos Bajo Stock
        public List<ProductoBajoStockDto> ProductosBajoStock { get; set; } = new();

        // Pedidos Recientes
        public List<PedidoRecienteDto> PedidosRecientes { get; set; } = new();
    }

    public class ResumenDiarioDto
    {
        public decimal VentasHoy { get; set; }
        public decimal VentasAyer { get; set; }
        public double PorcentajeVentas { get; set; }

        public int PedidosHoy { get; set; }
        public int PedidosAyer { get; set; }
        public double PorcentajePedidos { get; set; }

        public int EnviadosHoy { get; set; }
        public int EnviadosAyer { get; set; }
        public double PorcentajeEnviados { get; set; }

        public int StockBajo { get; set; }
    }

    public class AuditoriaEntryDto
    {
        public string UsuarioNombre { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class PedidoEstancadoDto
    {
        public int PedidoId { get; set; }
        public string ClienteNombre { get; set; }
        public DateTime Fecha { get; set; }
        public double HorasTranscurridas { get; set; }
    }

    public class PedidosEstancadosPaginadoDto
    {
        public List<PedidoEstancadoDto> Pedidos { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalRegistros { get; set; }
    }

    public class MovimientoStockDto
    {
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public int TipoMovimientoId { get; set; }
        public DateTime Fecha { get; set; }
        public string Observaciones { get; set; }
    }

    public class ProductoBajoStockDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string ImagenUrl { get; set; }
        public string Categoria { get; set; }
        public int Stock { get; set; }
    }

    public class PedidoRecienteDto
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Cliente { get; set; }
        public decimal Total { get; set; }
        public string EstadoPedido { get; set; }
        public int EstadoPedidoId { get; set; }
    }
}
