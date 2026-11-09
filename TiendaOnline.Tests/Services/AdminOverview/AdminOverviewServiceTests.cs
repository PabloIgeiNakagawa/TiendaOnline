using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.AdminOverview;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.AdminOverview
{
    /// <summary>
    /// El widget "Atención requerida" del panel admin: los únicos grupos que
    /// piden acción humana son el cobrado sin despachar hace más de 48hs y el
    /// pendiente con menos de 8hs de vida (el vencimiento automático mate a los
    /// Nuevo+Pendiente a las 24h).
    /// </summary>
    public class AdminOverviewServiceTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public async Task ObtenerPedidosEstancadosPaginadoAsync_DosGruposReales_QExcluyeFallecidos()
        {
            int idPagadoSinEnviar, idPorVencer, idVencidoMuerto, idApenasIniciado, idFresco;
            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed);

                var pagado = TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddHours(-50),
                    fechaEnPreparacion: DateTime.Now.AddHours(-49));
                idPagadoSinEnviar = pagado.PedidoId; // cobrado hace 49hs: pide despacho

                var porVencer = TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente,
                    fechaPedido: DateTime.Now.AddHours(-20));
                idPorVencer = porVencer.PedidoId; // 20hs de vida: quedan 4hs antes del job

                var vencido = TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente,
                    fechaPedido: DateTime.Now.AddHours(-30));
                idVencidoMuerto = vencido.PedidoId; // ya pasó las 24hs: como mucho un cadáver si el job demoró

                var apenas = TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente,
                    fechaPedido: DateTime.Now.AddHours(-10));
                idApenasIniciado = apenas.PedidoId; // 10hs de vida: todavía no urge

                var fresco = TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddHours(-3),
                    fechaEnPreparacion: DateTime.Now.AddHours(-2));
                idFresco = fresco.PedidoId; // cobrado hace 2hs: flujo normal
            }

            using var context = _db.CreateContext();
            var resultado = await new AdminOverviewService(context).ObtenerPedidosEstancadosPaginadoAsync(1);

            Assert.Equal(2, resultado.TotalRegistros);

            var ids = resultado.Pedidos.Select(p => p.PedidoId).ToList();
            Assert.Contains(idPagadoSinEnviar, ids);
            Assert.Contains(idPorVencer, ids);
            Assert.DoesNotContain(idVencidoMuerto, ids);
            Assert.DoesNotContain(idApenasIniciado, ids);
            Assert.DoesNotContain(idFresco, ids);

            // El "desde" del grupo pagado es el momento del cobro, no el del pedido:
            // las horas son desde FechaEnPreparacion (~49), no desde FechaPedido (~50).
            var filaPagado = resultado.Pedidos.Single(p => p.PedidoId == idPagadoSinEnviar);
            Assert.InRange(filaPagado.HorasTranscurridas, 48, 51);
        }

        [Fact]
        public async Task ObtenerResumenHomeAsync_SumaLaPlataCobrada_StockBajoSaleDeUnaConsulta()
        {
            // Regla de la tanda 08/11: la venta del panel es la cobrada (subtotal +
            // envío, coherente con el dashboard) y el stock bajo alimenta el número
            // del resumen y el listado desde UNA consulta (antes eran dos).
            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed, precio: 50, stock: 50);

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 2,
                    estado: EstadoPedido.Enviado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Today, // hoy siempre (el borde 00:00 cuenta >= como incluido)
                    fechaEnvio: DateTime.Now.AddHours(-26), costoEnvio: 25); // hoy: 2×50 + 25 = 125 (lo despacharon ayer)

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Entregado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Today.AddDays(-1).AddHours(12), costoEnvio: 40); // ayer al mediodía: 50 + 40 = 90

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 5,
                    estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente,
                    fechaPedido: DateTime.Now.AddHours(-3)); // hoy pendiente: no es venta

                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.Enviado, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddDays(-2),
                    fechaEnvio: DateTime.Today); // enviado hoy: suma a EnviadosHoy

                for (int i = 0; i < 7; i++)
                {
                    TestDatabase.SeedProducto(seed, stock: 2); // 7 productos bajo stock
                }
            }

            using var context = _db.CreateContext();
            var resultado = await new AdminOverviewService(context).ObtenerResumenHomeAsync();

            Assert.Equal(125m, resultado.ResumenDiario.VentasHoy);
            Assert.Equal(90m, resultado.ResumenDiario.VentasAyer);
            Assert.Equal(1, resultado.ResumenDiario.PedidosHoy);
            Assert.Equal(1, resultado.ResumenDiario.PedidosAyer);
            Assert.Equal(1, resultado.ResumenDiario.EnviadosHoy);

            // Número del resumen y listado salen de la MISMA fila de datos.
            Assert.Equal(7, resultado.ResumenDiario.StockBajo);
            Assert.Equal(5, resultado.ProductosBajoStock.Count);
        }

        [Fact]
        public async Task ObtenerResumenHomeAsync_MismoCache_DevuelveElMismoResultado()
        {
            // El home se guarda 30s: con el mismo cache, la segunda llamada
            // devuelve el mismo objeto armado (aunque la base cambie debajo);
            // sin cache, cada llamada arma el suyo y ve los cambios al instante.
            var cache = new MemoryCache(new MemoryCacheOptions());

            using var contextoPrimero = _db.CreateContext();
            var primero = await new AdminOverviewService(contextoPrimero, cache).ObtenerResumenHomeAsync();

            using (var seed = _db.CreateContext())
            {
                var cliente = TestDatabase.SeedUsuario(seed);
                var producto = TestDatabase.SeedProducto(seed);
                TestDatabase.SeedPedido(seed, cliente, producto, cantidad: 1,
                    estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                    fechaPedido: DateTime.Now.AddHours(-50),
                    fechaEnPreparacion: DateTime.Now.AddHours(-50));
            }

            var segundo = await new AdminOverviewService(_db.CreateContext(), cache).ObtenerResumenHomeAsync();
            Assert.Same(primero, segundo); // cache vigente: ni miró el pedido nuevos

            var fresco = await new AdminOverviewService(_db.CreateContext()).ObtenerResumenHomeAsync();
            Assert.NotSame(primero, fresco);
            Assert.Single(fresco.PedidosEstancados); // el pedido nuevo ahora está en la lista
        }
    }
}
