using System;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
