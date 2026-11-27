using System.Globalization;
using System.IO.Compression;
using System.Text;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Extensions;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Reportes
{
    /// <summary>
    /// Armado de los CSV del dashboard: una tabla por reporte, separador ";",
    /// coma decimal y fechas dd/MM fijadas a es-AR (no dependen de la cultura
    /// del server). El ZIP baja todos los reportes juntos, un archivo por tabla.
    /// </summary>
    public static class ReportesCsv
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

        private static readonly ReporteExportable[] Orden =
        {
            ReporteExportable.Resumen,
            ReporteExportable.VentasPorMes,
            ReporteExportable.EstadosPedidos,
            ReporteExportable.VentasPorCategoria,
            ReporteExportable.VentasPorMetodoPago,
            ReporteExportable.TopProductos,
            ReporteExportable.TopClientes,
            ReporteExportable.PedidosPorDiaHora,
            ReporteExportable.StockInmovilizado
        };

        public static IReadOnlyList<ReporteExportable> Todos => Orden;

        public static string Armar(ReporteExportable reporte, DashboardDto datos) => reporte switch
        {
            ReporteExportable.Resumen => ArmarResumen(datos.MetricasGenerales),
            ReporteExportable.VentasPorMes => ArmarVentasPorMes(datos.VentasPorMes),
            ReporteExportable.EstadosPedidos => ArmarEstadosPedidos(datos.EstadisticasPedidos),
            ReporteExportable.VentasPorCategoria => ArmarVentasPorCategoria(datos.VentasPorCategoria),
            ReporteExportable.VentasPorMetodoPago => ArmarVentasPorMetodoPago(datos.VentasPorMetodoDePago),
            ReporteExportable.TopProductos => ArmarTopProductos(datos.TopProductos),
            ReporteExportable.TopClientes => ArmarTopClientes(datos.TopClientes),
            ReporteExportable.PedidosPorDiaHora => ArmarPedidosPorDiaHora(datos.VentasPorDiaHora),
            ReporteExportable.StockInmovilizado => ArmarStockInmovilizado(datos.StockInmovilizado),
            _ => throw new ArgumentOutOfRangeException(nameof(reporte), reporte, "Reporte sin exportador.")
        };

        public static string Clave(ReporteExportable reporte) => reporte switch
        {
            ReporteExportable.Resumen => "resumen",
            ReporteExportable.VentasPorMes => "ventas-por-mes",
            ReporteExportable.EstadosPedidos => "estados-de-pedidos",
            ReporteExportable.VentasPorCategoria => "ventas-por-categoria",
            ReporteExportable.VentasPorMetodoPago => "ventas-por-metodo-de-pago",
            ReporteExportable.TopProductos => "top-productos",
            ReporteExportable.TopClientes => "top-clientes",
            ReporteExportable.PedidosPorDiaHora => "pedidos-por-dia-y-hora",
            ReporteExportable.StockInmovilizado => "stock-inmovilizado",
            _ => throw new ArgumentOutOfRangeException(nameof(reporte), reporte, "Reporte sin clave.")
        };

        public static string NombreDeArchivo(ReporteExportable reporte, DateTime fecha)
            => $"reporte-{Clave(reporte)}-{fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.csv";

        // BOM UTF-8: sin él Excel adivina ANSI y los acentos salen rotos.
        public static byte[] ConBom(string texto) => Csv.Archivo(texto);

        public static byte[] ArmarZip(DashboardDto datos)
        {
            using var memoria = new MemoryStream();
            using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var i = 0; i < Orden.Length; i++)
                {
                    var entrada = zip.CreateEntry($"{i + 1:00}-{Clave(Orden[i])}.csv");
                    using var stream = entrada.Open();
                    var bytes = ConBom(Armar(Orden[i], datos));
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            return memoria.ToArray();
        }

        private static string ArmarResumen(MetricasGeneralesDto metricas)
        {
            var sb = new StringBuilder();
            Fila(sb, "Métrica", "Valor");
            Fila(sb, "Ventas totales", metricas.VentasTotales.ToString("N2", Cultura));
            Fila(sb, "Ventas del mes", metricas.VentasMesActual.ToString("N2", Cultura));
            Fila(sb, "Pedidos totales", metricas.TotalPedidos.ToString(Cultura));
            Fila(sb, "Pedidos del mes", metricas.PedidosMesActual.ToString(Cultura));
            Fila(sb, "Clientes activos", metricas.TotalClientes.ToString(Cultura));
            Fila(sb, "Clientes nuevos del mes", metricas.ClientesMesActual.ToString(Cultura));
            Fila(sb, "Productos con stock bajo", metricas.ProductosBajoStock.ToString(Cultura));
            Fila(sb, "Ticket promedio", metricas.PromedioVentaPorPedido.ToString("N2", Cultura));
            Fila(sb, "Tiempo promedio de preparación (horas)", metricas.TiempoPromedioPreparacionHoras.ToString("N1", Cultura));
            Fila(sb, "Variación de ventas vs mes anterior (%)", metricas.PorcentajeCambioVentas.ToString("N1", Cultura));
            Fila(sb, "Variación de pedidos vs mes anterior (%)", metricas.PorcentajeCambioPedidos.ToString("N1", Cultura));
            Fila(sb, "Variación de clientes vs mes anterior (%)", metricas.PorcentajeCambioClientes.ToString("N1", Cultura));
            return sb.ToString();
        }

        private static string ArmarVentasPorMes(List<VentaPorMesDto> ventas)
        {
            var sb = new StringBuilder();
            Fila(sb, "Mes", "Pedidos", "Ventas");
            foreach (var venta in ventas)
            {
                Fila(sb,
                    venta.NombreMes,
                    venta.CantidadPedidos.ToString(Cultura),
                    venta.TotalVentas.ToString("N2", Cultura));
            }
            return sb.ToString();
        }

        private static string ArmarEstadosPedidos(EstadisticasPedidosDto estados)
        {
            var sb = new StringBuilder();
            Fila(sb, "Estado", "Pedidos", "Porcentaje del total");
            Fila(sb, EstadoPedido.Nuevo.GetDisplayName(), estados.TotalNuevos.ToString(Cultura), estados.PorcentajeNuevos.ToString("N1", Cultura));
            Fila(sb, EstadoPedido.EnPreparacion.GetDisplayName(), estados.TotalEnPreparacion.ToString(Cultura), estados.PorcentajeEnPreparacion.ToString("N1", Cultura));
            Fila(sb, EstadoPedido.Enviado.GetDisplayName(), estados.TotalEnviados.ToString(Cultura), estados.PorcentajeEnviados.ToString("N1", Cultura));
            Fila(sb, EstadoPedido.Entregado.GetDisplayName(), estados.TotalEntregados.ToString(Cultura), estados.PorcentajeEntregados.ToString("N1", Cultura));
            Fila(sb, EstadoPedido.Cancelado.GetDisplayName(), estados.TotalCancelados.ToString(Cultura), estados.PorcentajeCancelados.ToString("N1", Cultura));
            return sb.ToString();
        }

        private static string ArmarVentasPorCategoria(List<VentaPorCategoriaDto> ventas)
        {
            var sb = new StringBuilder();
            Fila(sb, "Categoría", "Productos vendidos", "Ventas", "Participación (%)");
            foreach (var venta in ventas)
            {
                Fila(sb,
                    Csv.NeutralizarFormula(venta.Categoria),
                    venta.CantidadProductos.ToString(Cultura),
                    venta.TotalVentas.ToString("N2", Cultura),
                    venta.PorcentajeDelTotal.ToString(Cultura));
            }
            return sb.ToString();
        }

        private static string ArmarVentasPorMetodoPago(List<VentaPorMetodoDePagoDto> ventas)
        {
            var sb = new StringBuilder();
            Fila(sb, "Método de pago", "Pedidos", "Ventas", "Participación (%)");
            foreach (var venta in ventas)
            {
                Fila(sb,
                    venta.MetodoDePago,
                    venta.CantidadPedidos.ToString(Cultura),
                    venta.TotalVentas.ToString("N2", Cultura),
                    venta.PorcentajeDelTotal.ToString(Cultura));
            }
            return sb.ToString();
        }

        private static string ArmarTopProductos(List<ProductoMasVendidoDto> productos)
        {
            var sb = new StringBuilder();
            Fila(sb, "Producto", "ID", "Categoría", "Unidades vendidas", "Subtotal vendido");
            foreach (var producto in productos)
            {
                Fila(sb,
                    Csv.NeutralizarFormula(producto.Nombre),
                    producto.ProductoId.ToString(Cultura),
                    Csv.NeutralizarFormula(producto.Categoria),
                    producto.CantidadVendida.ToString(Cultura),
                    producto.TotalVentas.ToString("N2", Cultura));
            }
            return sb.ToString();
        }

        private static string ArmarTopClientes(List<ClienteTopDto> clientes)
        {
            var sb = new StringBuilder();
            Fila(sb, "Cliente", "Email", "Pedidos", "Total gastado", "Última compra");
            foreach (var cliente in clientes)
            {
                Fila(sb,
                    Csv.NeutralizarFormula(cliente.NombreCompleto),
                    Csv.NeutralizarFormula(cliente.Email),
                    cliente.TotalPedidos.ToString(Cultura),
                    cliente.TotalGastado.ToString("N2", Cultura),
                    cliente.UltimaCompra.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static string ArmarPedidosPorDiaHora(List<VentasPorDiaHoraDto> filas)
        {
            var sb = new StringBuilder();
            Fila(sb, "Día", "Madrugada", "Mañana", "Tarde", "Noche", "Total");
            foreach (var fila in filas.OrderBy(f => f.OrdenDia))
            {
                Fila(sb,
                    fila.DiaSemana,
                    fila.Madrugada.ToString(Cultura),
                    fila.Manana.ToString(Cultura),
                    fila.Tarde.ToString(Cultura),
                    fila.Noche.ToString(Cultura),
                    (fila.Madrugada + fila.Manana + fila.Tarde + fila.Noche).ToString(Cultura));
            }
            return sb.ToString();
        }

        private static string ArmarStockInmovilizado(List<StockInmovilizadoDto> stock)
        {
            var sb = new StringBuilder();
            Fila(sb, "Producto", "ID", "Categoría", "Stock", "Precio", "Valor invertido", "Última venta", "Días sin venta");
            foreach (var producto in stock)
            {
                Fila(sb,
                    Csv.NeutralizarFormula(producto.Nombre),
                    producto.ProductoId.ToString(Cultura),
                    Csv.NeutralizarFormula(producto.Categoria),
                    producto.Stock.ToString(Cultura),
                    producto.Precio.ToString("N2", Cultura),
                    producto.ValorInvertido.ToString("N2", Cultura),
                    producto.UltimaVenta?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Nunca",
                    producto.DiasSinVenta?.ToString(Cultura) ?? "");
            }
            return sb.ToString();
        }

        private static void Fila(StringBuilder sb, params string[] celdas)
            => sb.AppendLine(string.Join(';', celdas.Select(Csv.Celda)));
    }
}
