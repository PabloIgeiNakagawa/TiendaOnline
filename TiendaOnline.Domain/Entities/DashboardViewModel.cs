public class DashboardViewModel
{
    public List<string> TopProductos { get; set; }
    public List<int> VentasPorProducto { get; set; }

    public List<string> EstadosPedido { get; set; }
    public List<int> CantidadPorEstado { get; set; }

    public List<string> Categorias { get; set; }
    public List<decimal> VentasPorCategoria { get; set; }

    public List<string> TopClientes { get; set; }
    public List<int> PedidosPorCliente { get; set; }

    public int CantidadCancelados { get; set; }
    public decimal PorcentajeCancelados { get; set; }
}
