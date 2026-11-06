using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Tests de ConfirmarPagoAsync: el corazón del flujo de dinero.
    /// Idempotencia, integridad de montos, reserva de stock y estados,
    /// siempre con el motivo tipado del resultado (no basta el bool).
    /// </summary>
    public class PedidoCommandServiceConfirmarPagoTests
    {
        private static InfoPagoDto Info(int pedidoId, string estado, decimal monto, string transaccion = "tx-1")
            => new() { PedidoId = pedidoId, Estado = estado, MontoPagado = monto, TransaccionId = transaccion };

        // ------------------------------------------------------------------
        // Casos de rechazo sin cambios
        // ------------------------------------------------------------------

        [Fact]
        public async Task ConfirmarPago_PedidoInexistente_FallaPedidoNoEncontrado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(99999, "approved", 100m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.PedidoNoEncontrado, resultado.Motivo);
        }

        [Fact]
        public async Task ConfirmarPago_YaAprobado_RetornaYaConfirmado_SinCambios()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                transaccionId: "tx-orig");
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "approved", 200m));

            // Retry benigno: está en el bucket de éxito (el cliente puede avanzar) sin re-procesar.
            Assert.True(resultado.Confirmado);
            Assert.Equal(MotivoPago.YaConfirmado, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Aprobado, pedidoDb.EstadoPago);
            Assert.Equal("tx-orig", pedidoDb.TransaccionPagoId); // no pisa la transacción ya registrada

            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock); // no descuenta stock dos veces
        }

        [Fact]
        public async Task ConfirmarPago_MontoDivergente_FallaMontoNoCoincide_NoConfirma()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, costoEnvio: 5m);
            var sut = Sut.PedidoCommand(ctx);

            // MP reporta un monto mayor al que el sistema calculó (200 + 5 = 205)
            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "approved", 210m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.MontoNoCoincide, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Pendiente, pedidoDb.EstadoPago);

            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock);
        }

        [Fact]
        public async Task ConfirmarPago_StockInsuficiente_FallaStockInsuficiente_YNoDescuenta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 1);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 3);
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "approved", 300m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.StockInsuficiente, resultado.Motivo);

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(1, stock);

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Pendiente, pedidoDb.EstadoPago);
        }

        [Fact]
        public async Task ConfirmarPago_PedidoVencido_FallaPedidoVencido()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                fechaPedido: DateTime.Now.AddHours(-25)); // > TiempoMaximoPagoPendiente (24h)
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "approved", 200m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.PedidoVencido, resultado.Motivo);

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock);
        }

        [Fact]
        public async Task ConfirmarPago_AprobacionSobrePedidoCancelado_FallaEstadoIncoherente()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                estado: EstadoPedido.Cancelado);
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "approved", 200m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.EstadoIncoherente, resultado.Motivo);

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock);
        }

        [Fact]
        public async Task ConfirmarPago_EstadoMpNoManejado_FallaEstadoNoManejado_SinCambios()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);
            var sut = Sut.PedidoCommand(ctx);

            // MP manda "in_process" (pago a confirmar): sin acción definida, nada toca nada.
            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "in_process", 200m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.EstadoNoManejado, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Pendiente, pedidoDb.EstadoPago);
        }

        // ------------------------------------------------------------------
        // Flujo feliz y demás estados de MP
        // ------------------------------------------------------------------

        [Fact]
        public async Task ConfirmarPago_MontoExacto_ConfirmaYGeneraSalidaVenta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, costoEnvio: 5m);
            var sut = Sut.PedidoCommand(ctx);

            var total = 2 * 100m + 5m;
            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "approved", total));

            Assert.True(resultado.Confirmado);
            Assert.Equal(MotivoPago.Confirmado, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Aprobado, pedidoDb.EstadoPago);
            Assert.Equal(EstadoPedido.EnPreparacion, pedidoDb.Estado);
            Assert.NotNull(pedidoDb.FechaEnPreparacion);
            Assert.Equal("tx-1", pedidoDb.TransaccionPagoId);

            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(3, stock); // 5 - 2

            var movimiento = await verify.MovimientosStock
                .Where(m => m.PedidoId == pedido.PedidoId)
                .SingleAsync();
            Assert.Equal(TipoMovimiento.SalidaVenta, movimiento.Tipo);
            Assert.Equal(-2, movimiento.Cantidad);
        }

        [Fact]
        public async Task ConfirmarPago_Rechazado_MarcaEstadoPagoRechazado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "rejected", 200m));

            // "Confirmado" para MP significa "señal aplicada", acá el pago quedó rechazado.
            Assert.True(resultado.Confirmado);
            Assert.Equal(MotivoPago.Confirmado, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Rechazado, pedidoDb.EstadoPago);
            Assert.Equal(EstadoPedido.Nuevo, pedidoDb.Estado); // rechazo no cancela el pedido

            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock); // rechazado no toca stock
        }

        [Fact]
        public async Task ConfirmarPago_Cancelled_MarcaPedidoCancelado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "cancelled", 200m));

            Assert.True(resultado.Confirmado);
            Assert.Equal(MotivoPago.Confirmado, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Rechazado, pedidoDb.EstadoPago);
            Assert.Equal(EstadoPedido.Cancelado, pedidoDb.Estado);
            Assert.NotNull(pedidoDb.FechaCancelado);
        }

        [Fact]
        public async Task ConfirmarPago_Refunded_SobrePedidoNoAprobado_FallaEstadoIncoherente()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "refunded", 200m));

            Assert.False(resultado.Confirmado);
            Assert.Equal(MotivoPago.EstadoIncoherente, resultado.Motivo);
        }

        [Fact]
        public async Task ConfirmarPago_Refunded_CuandoAprobado_MarcaEstadoPagoReembolsado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado);
            var sut = Sut.PedidoCommand(ctx);

            var resultado = await sut.ConfirmarPagoAsync(Info(pedido.PedidoId, "refunded", 200m));

            Assert.True(resultado.Confirmado);
            Assert.Equal(MotivoPago.Confirmado, resultado.Motivo);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Reembolsado, pedidoDb.EstadoPago);
        }
    }
}
