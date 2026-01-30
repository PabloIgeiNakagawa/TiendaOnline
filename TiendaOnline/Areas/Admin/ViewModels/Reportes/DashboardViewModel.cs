namespace TiendaOnline.Areas.Admin.ViewModels.Reportes
{
    // ViewModel principal del Dashboard
    public class DashboardViewModel
    {
        // Métricas generales
        public MetricasGenerales MetricasGenerales { get; set; }

        // Gráficos y reportes
        public List<ProductoMasVendido> TopProductos { get; set; }
        public List<ClienteTopViewModel> TopClientes { get; set; }
        public List<VentaPorCategoria> VentasPorCategoria { get; set; }
        public List<VentaPorMes> VentasPorMes { get; set; }
        public EstadisticasPedidos EstadisticasPedidos { get; set; }
        public List<ProductoBajoStock> ProductosBajoStock { get; set; }
        public List<PedidoReciente> PedidosRecientes { get; set; }
    }


    // Métricas generales del negocio
    public class MetricasGenerales
    {
        public decimal VentasTotales { get; set; }
        public decimal VentasMesActual { get; set; }
        public int TotalPedidos { get; set; }
        public int PedidosMesActual { get; set; }
        public int TotalClientes { get; set; }
        public int ClientesMesActual { get; set; }
        public int ProductosBajoStock { get; set; }
        public decimal PromedioVentaPorPedido { get; set; }

        // Comparaciones con mes anterior
        public decimal PorcentajeCambioVentas { get; set; }
        public decimal PorcentajeCambioPedidos { get; set; }
        public decimal PorcentajeCambioClientes { get; set; }
    }

    // Producto más vendido
    public class ProductoMasVendido
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
        public string ImagenUrl { get; set; }
    }

    // Cliente con más compras
    public class ClienteTopViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public int TotalPedidos { get; set; }
        public decimal TotalGastado { get; set; }
        public DateTime UltimaCompra { get; set; }
    }

    // Ventas por categoría
    public class VentaPorCategoria
    {
        public string Categoria { get; set; }
        public int CantidadProductos { get; set; }
        public decimal TotalVentas { get; set; }
        public int PorcentajeDelTotal { get; set; }
    }

    // Ventas por mes (últimos 12 meses)
    public class VentaPorMes
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string NombreMes { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadPedidos { get; set; }
    }

    // Estadísticas de estados de pedidos
    public class EstadisticasPedidos
    {
        public int TotalPendientes { get; set; }
        public int TotalEnviados { get; set; }
        public int TotalEntregados { get; set; }
        public int TotalCancelados { get; set; }
        public decimal PorcentajePendientes { get; set; }
        public decimal PorcentajeEnviados { get; set; }
        public decimal PorcentajeEntregados { get; set; }
        public decimal PorcentajeCancelados { get; set; }
    }

    // Producto con bajo stock
    public class ProductoBajoStock
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int Stock { get; set; }
        public int CantidadVendidaUltimoMes { get; set; }
        public string ImagenUrl { get; set; }
    }

    // Pedido reciente para la tabla
    public class PedidoReciente
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Cliente { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public int EstadoNumero { get; set; }
    }
}