using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Tests de la máquina de estados de Pedido (Enviado / Entregado / Cancelado):
    /// transiciones inválidas y devolución de stock con movimiento CancelacionPedido.
    /// </summary>
    public class PedidoCommandServiceTransicionesTests
    {
        [Fact]
        public async Task PedidoEnviado_DesdeNuevo_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.Nuevo); // solo se puede enviar desde EnPreparacion
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(() => sut.PedidoEnviadoAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Nuevo, pedidoDb.Estado);
        }

        [Fact]
        public async Task PedidoEntregado_DesdeEnPreparacion_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.EnPreparacion); // solo se puede entregar desde Enviado
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(() => sut.PedidoEntregadoAsync(pedido.PedidoId));
        }

        [Fact]
        public async Task PedidoCancelado_DePedidoAprobado_DevuelveStock_YGeneraMovimiento()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 3,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado);
            var sut = Sut.PedidoCommand(ctx);

            await sut.PedidoCanceladoAsync(pedido.PedidoId); // no debe lanzar

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(13, stock); // 10 + 3 devueltos

            var movimiento = await verify.MovimientosStock
                .Where(m => m.PedidoId == pedido.PedidoId)
                .SingleAsync();
            Assert.Equal(TipoMovimiento.CancelacionPedido, movimiento.Tipo);
            Assert.Equal(3, movimiento.Cantidad);

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Cancelado, pedidoDb.Estado);
        }

        [Fact]
        public async Task PedidoCancelado_SinPagar_NoDevuelveStock()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 3,
                estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente);
            var sut = Sut.PedidoCommand(ctx);

            await sut.PedidoCanceladoAsync(pedido.PedidoId);

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(10, stock); // nunca se reservó stock (no estaba pagado)

            var movimientos = await verify.MovimientosStock
                .Where(m => m.PedidoId == pedido.PedidoId)
                .ToListAsync();
            Assert.Empty(movimientos);
        }

        [Fact]
        public async Task PedidoCancelado_YaCancelado_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.Cancelado);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(() => sut.PedidoCanceladoAsync(pedido.PedidoId));
        }
    }
}
