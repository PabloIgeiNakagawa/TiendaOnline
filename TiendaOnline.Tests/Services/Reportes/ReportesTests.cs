using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
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

        [Fact]
        public async Task ObtenerDatosAsync_LaVentaEsSubtotalMasEnvio_YElTicketPromedioDividePorCobrados()
        {
            // Regla de la tanda 08/11: la venta es la plata REAL cobrada (MP cobra
            // subtotal + envío) y el ticket promedio reparte entre pedidos QUE
            // PAGARON — el pendiente con su envío imaginario no infla nada.
            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed, precio: 100, stock: 50);

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Enviado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddDays(-3), costoEnvio: 50); // venta: 100 + 50

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 3,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente,
                    fechaPedido: DateTime.Now.AddHours(-5), costoEnvio: 999); // pendiente: no existe para la venta
            }

            using var context = _db.CreateContext();
            var datos = await new ReportesService(context).ObtenerDatosAsync();

            Assert.Equal(150m, datos.MetricasGenerales.VentasTotales);
            Assert.Equal(150m, datos.MetricasGenerales.PromedioVentaPorPedido);

            var venta = Assert.Single(datos.VentasPorMetodoDePago);
            Assert.Equal(150m, venta.TotalVentas);
        }

        [Fact]
        public async Task ObtenerDatosAsync_ElMapaCalendario_MiraSoloElUltimoAnio()
        {
            // El mapa día/franja describe la operación actual: un pedido del año
            // pasado (cobrado) no entra.
            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed, precio: 100, stock: 50);

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Entregado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddMonths(-13)); // fuera de la ventana

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Entregado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddDays(-1)); // adentro
            }

            using var context = _db.CreateContext();
            var datos = await new ReportesService(context).ObtenerDatosAsync();

            var pedidosEnElMapa = datos.VentasPorDiaHora.Sum(v => v.Madrugada + v.Manana + v.Tarde + v.Noche);
            Assert.Equal(1, pedidosEnElMapa);
        }

        [Fact]
        public async Task ObtenerDatosAsync_ProductoNuncaVendido_NoFingeNoventaDias()
        {
            // DiasSinVenta es null cuando no hay ventas: la vista muestra "Nunca",
            // no el default mentiroso de 90 que el front leía como "hace 90 días".
            using (var seed = _db.CreateContext())
            {
                TestDatabase.SeedProducto(seed, stock: 3);
            }

            using var context = _db.CreateContext();
            var datos = await new ReportesService(context).ObtenerDatosAsync();

            var stock = Assert.Single(datos.StockInmovilizado);
            Assert.Null(stock.DiasSinVenta);
            Assert.Null(stock.UltimaVenta);
        }

        [Fact]
        public async Task ObtenerDatosAsync_MismoCache_DevuelveElMismoDatos_CacheNuevo_RecorreOtraVez()
        {
            // El dashboard se guarda 60s: con el mismo cache, una segunda llamada
            // (aunque la base cambie debajo) devuelve el MISMO objeto armado; con
            // un cache nuevo, el armado re-corre consultas y ve los cambios.
            var cache = new MemoryCache(new MemoryCacheOptions());

            using var context = _db.CreateContext();
            var datosPrimero = await new ReportesService(context, cache).ObtenerDatosAsync();

            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed, precio: 100, stock: 50);
                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Enviado, estadoPago: EstadoPago.Aprobado);
            }

            var datosSegundo = await new ReportesService(_db.CreateContext(), cache).ObtenerDatosAsync();
            Assert.Same(datosPrimero, datosSegundo); // cache vigente: ni miró el cambio

            var datosFrescos = await new ReportesService(_db.CreateContext(), new MemoryCache(new MemoryCacheOptions())).ObtenerDatosAsync();
            Assert.NotSame(datosPrimero, datosFrescos); // cache nuevo: re-corrió consultas
            Assert.True(datosFrescos.MetricasGenerales.VentasTotales >= datosPrimero.MetricasGenerales.VentasTotales);
        }
    }
}
