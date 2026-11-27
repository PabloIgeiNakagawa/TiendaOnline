namespace TiendaOnline.Features.Reportes
{
    /// <summary>
    /// Reportes del dashboard que se pueden bajar como CSV. La clave viaja en
    /// la URL del export (?reporte=...) y ordena los archivos dentro del ZIP.
    /// </summary>
    public enum ReporteExportable
    {
        Resumen,
        VentasPorMes,
        EstadosPedidos,
        VentasPorCategoria,
        VentasPorMetodoPago,
        TopProductos,
        TopClientes,
        PedidosPorDiaHora,
        StockInmovilizado
    }
}
