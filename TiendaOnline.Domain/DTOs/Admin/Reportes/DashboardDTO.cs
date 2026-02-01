namespace TiendaOnline.Domain.DTOs.Admin.Reportes
{
    public class DashboardDTO
    {
        public MetricasGeneralesDto MetricasGenerales { get; set; }
        public List<ProductoMasVendidoDto> TopProductos { get; set; }
        public List<ClienteTopDto> TopClientes { get; set; }
        public List<VentaPorCategoriaDto> VentasPorCategoria { get; set; }
        public List<VentaPorMesDto> VentasPorMes { get; set; }
        public EstadisticasPedidosDto EstadisticasPedidos { get; set; }
        public List<ProductoBajoStockDto> ProductosBajoStock { get; set; }
        public List<PedidoRecienteDto> PedidosRecientes { get; set; }
    }

    public class MetricasGeneralesDto
    {
        public decimal VentasTotales { get; set; }
        public decimal VentasMesActual { get; set; }
        public int TotalPedidos { get; set; }
        public int PedidosMesActual { get; set; }
        public int TotalClientes { get; set; }
        public int ClientesMesActual { get; set; }
        public int ProductosBajoStock { get; set; }
        public decimal PromedioVentaPorPedido { get; set; }
        public decimal PorcentajeCambioVentas { get; set; }
        public decimal PorcentajeCambioPedidos { get; set; }
        public decimal PorcentajeCambioClientes { get; set; }
    }

    public class ProductoMasVendidoDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
        public string ImagenUrl { get; set; }
    }

    public class ClienteTopDto
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public int TotalPedidos { get; set; }
        public decimal TotalGastado { get; set; }
        public DateTime UltimaCompra { get; set; }
    }

    public class VentaPorCategoriaDto
    {
        public string Categoria { get; set; }
        public int CantidadProductos { get; set; }
        public decimal TotalVentas { get; set; }
        public int PorcentajeDelTotal { get; set; }
    }

    public class VentaPorMesDto
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string NombreMes { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadPedidos { get; set; }
    }

    public class EstadisticasPedidosDto
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

    public class ProductoBajoStockDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int Stock { get; set; }
        public int CantidadVendidaUltimoMes { get; set; }
        public string ImagenUrl { get; set; }
    }

    public class PedidoRecienteDto
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Cliente { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public int EstadoNumero { get; set; }
    }
}
