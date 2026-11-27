using System.IO.Compression;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Features.Reportes;
using Xunit;

namespace TiendaOnline.Tests.Services.Reportes
{
    /// <summary>
    /// CSV de los reportes del dashboard: formato es-AR fijo (coma decimal,
    /// miles con punto, fechas dd/MM), escapado de celdas y ZIP con una tabla
    /// por reporte.
    /// </summary>
    public class ReportesCsvTests
    {
        [Fact]
        public void Armar_Resumen_FormateaDineroYPorcentajesEnEsAr()
        {
            var datos = new DashboardDto
            {
                MetricasGenerales = new MetricasGeneralesDto
                {
                    VentasTotales = 1234.5m,
                    VentasMesActual = 1000m,
                    TotalPedidos = 12,
                    PedidosMesActual = 3,
                    TotalClientes = 7,
                    ClientesMesActual = 2,
                    ProductosBajoStock = 4,
                    PromedioVentaPorPedido = 102.88m,
                    TiempoPromedioPreparacionHoras = 2.5,
                    PorcentajeCambioVentas = 12.34m,
                    PorcentajeCambioPedidos = -5.5m,
                    PorcentajeCambioClientes = 0m
                }
            };

            var csv = ReportesCsv.Armar(ReporteExportable.Resumen, datos);

            Assert.StartsWith("Métrica;Valor", csv);
            Assert.Contains("Ventas totales;1.234,50", csv);
            Assert.Contains("Pedidos totales;12", csv);
            Assert.Contains("Ticket promedio;102,88", csv);
            Assert.Contains("Tiempo promedio de preparación (horas);2,5", csv);
            Assert.Contains("Variación de ventas vs mes anterior (%);12,3", csv);
            Assert.Contains("Variación de pedidos vs mes anterior (%);-5,5", csv);
        }

        [Fact]
        public void Armar_StockInmovilizado_NuncaVendidoNoFingeDias()
        {
            var datos = new DashboardDto
            {
                StockInmovilizado =
                {
                    new StockInmovilizadoDto
                    {
                        ProductoId = 1,
                        Nombre = "Nunca vendido",
                        Categoria = "Cat",
                        Stock = 2,
                        Precio = 10m,
                        ValorInvertido = 20m,
                        UltimaVenta = null,
                        DiasSinVenta = null
                    },
                    new StockInmovilizadoDto
                    {
                        ProductoId = 2,
                        Nombre = "Viejo",
                        Categoria = "Cat",
                        Stock = 1,
                        Precio = 5m,
                        ValorInvertido = 5m,
                        UltimaVenta = new DateTime(2026, 1, 5),
                        DiasSinVenta = 12
                    }
                }
            };

            var csv = ReportesCsv.Armar(ReporteExportable.StockInmovilizado, datos);

            Assert.Contains("Nunca vendido;1;Cat;2;10,00;20,00;Nunca;", csv);
            Assert.Contains("Viejo;2;Cat;1;5,00;5,00;05/01/2026;12", csv);
        }

        [Fact]
        public void Armar_TopClientes_IncluyeEmailYFechaDeUltimaCompra()
        {
            var datos = new DashboardDto
            {
                TopClientes =
                {
                    new ClienteTopDto
                    {
                        UsuarioId = 1,
                        NombreCompleto = "Ana Pérez",
                        Email = "ana@test.local",
                        TotalPedidos = 2,
                        TotalGastado = 1500.5m,
                        UltimaCompra = new DateTime(2026, 11, 20)
                    }
                }
            };

            var csv = ReportesCsv.Armar(ReporteExportable.TopClientes, datos);

            Assert.Contains("Ana Pérez;ana@test.local;2;1.500,50;20/11/2026", csv);
        }

        [Fact]
        public void Armar_CeldaConSeparadorYComillas_SeEscapa()
        {
            var datos = new DashboardDto
            {
                TopProductos =
                {
                    new ProductoMasVendidoDto
                    {
                        ProductoId = 9,
                        Nombre = "Prod; \"especial\"",
                        Categoria = "Cat",
                        CantidadVendida = 1,
                        TotalVentas = 10m
                    }
                }
            };

            var csv = ReportesCsv.Armar(ReporteExportable.TopProductos, datos);

            Assert.Contains("\"Prod; \"\"especial\"\"\";9;Cat;1;10,00", csv);
        }

        [Fact]
        public void Armar_TextoQueExcelTomariaComoFormula_SePrefijaConApostrofo()
        {
            var datos = new DashboardDto
            {
                TopProductos =
                {
                    new ProductoMasVendidoDto
                    {
                        ProductoId = 9,
                        Nombre = "=HYPERLINK(\"http://malo\",\"Click\")",
                        Categoria = "@SUM(A1)",
                        CantidadVendida = 1,
                        TotalVentas = 10m
                    }
                }
            };

            var csv = ReportesCsv.Armar(ReporteExportable.TopProductos, datos);

            // Excel evalúa las celdas que arrancan con =, +, - o @; el apóstrofo
            // las convierte en texto y no se muestra en la planilla. Los números
            // negativos no pasan por acá (siguen siendo números).
            Assert.Contains("'=HYPERLINK(", csv);
            Assert.Contains("'@SUM(A1)", csv);
        }

        [Fact]
        public void ArmarZip_ContieneUnCsvConBomPorCadaReporte()
        {
            var zip = ReportesCsv.ArmarZip(new DashboardDto());

            using var archivo = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);

            Assert.Equal(ReportesCsv.Todos.Count, archivo.Entries.Count);
            Assert.Equal("01-resumen.csv", archivo.Entries[0].Name);

            foreach (var entrada in archivo.Entries)
            {
                using var stream = entrada.Open();
                using var memoria = new MemoryStream();
                stream.CopyTo(memoria);
                var bytes = memoria.ToArray();

                Assert.True(bytes.Length > 3);
                Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
            }
        }

        [Fact]
        public void NombreDeArchivo_UsaLaClaveYLaFechaDelReporte()
        {
            Assert.Equal(
                "reporte-ventas-por-metodo-de-pago-20261126.csv",
                ReportesCsv.NombreDeArchivo(ReporteExportable.VentasPorMetodoPago, new DateTime(2026, 11, 26)));
        }
    }
}
