using System.Threading.Tasks;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Reportes;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Reportes
{
    /// <summary>
    /// El dashboard de reportes armado contra datos ya vividos: las métricas se
    /// calculan en memoria con entidades de contexto fresco, así que un Include
    /// faltante en el fetch no lo tapa nada (en producción cada request arranca
    /// sin nada tracked).
    /// </summary>
    public class ReportesTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public async Task ObtenerStockInmovilizadoAsync_ContextoFresco_NombraLaCategoria()
        {
            // Seed en un contexto y lectura en OTRO: si el fetch del armado no
            // Include a la categoría, p.Categoria queda NULL y p.Categoria.Nombre
            // tira NullReferenceException (fue el 500 del dashboard en producción).
            using (var seed = _db.CreateContext())
            {
                TestDatabase.SeedProducto(seed, stock: 5);
            }

            using var context = _db.CreateContext();
            var datos = await new ReportesService(context).ObtenerDatosAsync();

            var stock = Assert.Single(datos.StockInmovilizado);
            Assert.Equal(5, stock.Stock);
            Assert.False(string.IsNullOrWhiteSpace(stock.Categoria));
            Assert.Equal("Categoria Test", stock.Categoria);
        }

        [Fact]
        public async Task ObtenerDatosAsync_SoloLosPagosAprobados_CuentanComoVenta()
        {
            // Regla del dinero: "venta" = EstadoPago Aprobado. Un pendiente
            // (transferencia no pagada) y un rechazado no suman a los totales.
            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed, precio: 100, stock: 50);

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Enviado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddDays(-5)); // venta real: $100

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 3,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente,
                    fechaPedido: DateTime.Now.AddHours(-5)); // pendiente: no vende

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 2,
                    estado: EstadoPedido.Cancelado, estadoPago: EstadoPago.Rechazado,
                    fechaPedido: DateTime.Now.AddDays(-2)); // rechazado: no vende
            }

            using var context = _db.CreateContext();
            var datos = await new ReportesService(context).ObtenerDatosAsync();

            Assert.Equal(100m, datos.MetricasGenerales.VentasTotales);

            var venta = Assert.Single(datos.VentasPorMetodoDePago);
            Assert.Equal(1, venta.CantidadPedidos);
            Assert.Equal(100m, venta.TotalVentas);
        }
    }
}
