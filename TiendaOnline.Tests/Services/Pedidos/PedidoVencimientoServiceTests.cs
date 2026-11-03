using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Tests del job de Hangfire que cancela pedidos pendientes de pago vencidos (24h).
    /// </summary>
    public class PedidoVencimientoServiceTests
    {
        [Fact]
        public async Task CancelarPedidosVencidos_PedidoSinPagarMasDe24h_LoCancela()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estadoPago: EstadoPago.Pendiente,
                fechaPedido: DateTime.Now.AddHours(-25)); // > TiempoMaximoPagoPendiente (24h)
            var sut = Sut.PedidoVencimiento(ctx);

            var cantidadCancelados = await sut.CancelarPedidosVencidosAsync();

            Assert.Equal(1, cantidadCancelados);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Cancelado, pedidoDb.Estado);
            Assert.NotNull(pedidoDb.FechaCancelado);
        }

        [Fact]
        public async Task CancelarPedidosVencidos_PedidoAprobado_NoLoCancela()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                fechaPedido: DateTime.Now.AddHours(-25));
            var sut = Sut.PedidoVencimiento(ctx);

            var cantidadCancelados = await sut.CancelarPedidosVencidosAsync();

            Assert.Equal(0, cantidadCancelados);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.EnPreparacion, pedidoDb.Estado); // intacto
        }

        [Fact]
        public async Task CancelarPedidosVencidos_MixSoloCancelaVencidos()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 10);

            var vencido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                fechaPedido: DateTime.Now.AddHours(-25));
            var reciente = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                fechaPedido: DateTime.Now.AddHours(-2));
            var aprobado = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                fechaPedido: DateTime.Now.AddHours(-25));
            var sut = Sut.PedidoVencimiento(ctx);

            var cantidadCancelados = await sut.CancelarPedidosVencidosAsync();

            Assert.Equal(1, cantidadCancelados);

            using var verify = db.CreateContext();
            Assert.Equal(EstadoPedido.Cancelado,
                (await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == vencido.PedidoId)).Estado);
            Assert.Equal(EstadoPedido.Nuevo,
                (await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == reciente.PedidoId)).Estado);
            Assert.Equal(EstadoPedido.EnPreparacion,
                (await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == aprobado.PedidoId)).Estado);
        }
    }
}
