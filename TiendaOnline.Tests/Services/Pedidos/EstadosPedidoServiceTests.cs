using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Tests.TestSupport;
using Hangfire;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Tests de la máquina de estados de Pedido (Enviado / Entregado / Cancelado):
    /// transiciones inválidas y devolución de stock con movimiento CancelacionPedido.
    /// </summary>
    public class EstadosPedidoServiceTests
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
            var sut = Sut.EstadosPedido(ctx);

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
            var sut = Sut.EstadosPedido(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(() => sut.PedidoEntregadoAsync(pedido.PedidoId));
        }

        [Fact]
        public async Task PedidoEntregado_DesdeEnviado_MarcaEstadoYFechaEntrega()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.Enviado);
            var sut = Sut.EstadosPedido(ctx);

            await sut.PedidoEntregadoAsync(pedido.PedidoId); // no debe lanzar

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Entregado, pedidoDb.Estado);
            Assert.NotNull(pedidoDb.FechaEntrega);
        }

        [Fact]
        public async Task PedidoEntregado_DesdeEnviado_EncolaAvisoAlCliente()
        {
            // Al entregar, el cliente recibe el "Tu pedido fue entregado" por
            // Hangfire: la cola de email crece en exactamente uno y el job
            // apunta al caso de uso del aviso de entrega.
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.Enviado);
            var sut = Sut.EstadosPedido(ctx);

            var api = JobStorage.Current.GetMonitoringApi();

            // Delta contando SOLO los jobs de este aviso: otros tests encolan
            // otros emails en paralelo, la cola entera no es un contador estable.
            int jobsDeEntrega() => api.EnqueuedJobs("default", 0, int.MaxValue)
                .Count(j => j.Value.Job.Method.Name == "EnviarEmailPedidoEntregadoAsync");
            var antes = jobsDeEntrega();

            await sut.PedidoEntregadoAsync(pedido.PedidoId);

            Assert.Equal(antes + 1, jobsDeEntrega());
        }

        [Fact]
        public async Task PedidoCancelado_DePedidoAprobado_DevuelveStock_MarcaReembolsado_YAvisaAlCliente()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 3,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado);
            var sut = Sut.EstadosPedido(ctx);

            var api = JobStorage.Current.GetMonitoringApi();
            int avisosDeReembolso() => api.EnqueuedJobs("default", 0, int.MaxValue)
                .Count(j => j.Value.Job.Method.Name == "EnviarEmailPagoReembolsadoAsync");
            var antes = avisosDeReembolso();

            var quedoReembolsado = await sut.PedidoCanceladoAsync(pedido.PedidoId); // no debe lanzar

            Assert.True(quedoReembolsado); // estaba cobrado: el dinero se devolvió
            Assert.Equal(antes + 1, avisosDeReembolso()); // el cliente recibe el aviso de reembolso

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
            // El dinero entró y salió: no es "Rechazado" (ese miente y borra el
            // pedido de los reportes que cuentan por Aprobado)
            Assert.Equal(EstadoPago.Reembolsado, pedidoDb.EstadoPago);
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
            var sut = Sut.EstadosPedido(ctx);

            var api = JobStorage.Current.GetMonitoringApi();
            int avisosDeReembolso() => api.EnqueuedJobs("default", 0, int.MaxValue)
                .Count(j => j.Value.Job.Method.Name == "EnviarEmailPagoReembolsadoAsync");
            var antes = avisosDeReembolso();

            var quedoReembolsado = await sut.PedidoCanceladoAsync(pedido.PedidoId);

            Assert.False(quedoReembolsado); // nunca se cobró
            Assert.Equal(antes, avisosDeReembolso()); // sin plata devuelta no va aviso de reembolso

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(10, stock); // nunca se reservó stock (no estaba pagado)

            var movimientos = await verify.MovimientosStock
                .Where(m => m.PedidoId == pedido.PedidoId)
                .ToListAsync();
            Assert.Empty(movimientos);

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            // Un pago pendiente que muere queda rechazado (el reembolso es de plata cobrada)
            Assert.Equal(EstadoPago.Rechazado, pedidoDb.EstadoPago);
        }

        [Fact]
        public async Task PedidoEnviado_DobleClick_ElSegundoLanzaConElEstadoReal()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.EnPreparacion);
            var sut = Sut.EstadosPedido(ctx);

            await sut.PedidoEnviadoAsync(pedido.PedidoId); // el primero gana el claim

            // El segundo pierde el claim y corta con el estado que el ganador dejó
            var ex = await Assert.ThrowsAsync<PedidoNoCancelableException>(
                () => sut.PedidoEnviadoAsync(pedido.PedidoId));
            Assert.Contains("Enviado", ex.Message);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Enviado, pedidoDb.Estado); // una sola transición
        }

        [Fact]
        public async Task PedidoEntregado_DobleClick_EncolaElAvisoUnaSolaVez()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1,
                estado: EstadoPedido.Enviado);
            var sut = Sut.EstadosPedido(ctx);

            var api = JobStorage.Current.GetMonitoringApi();
            int avisosDeEntrega() => api.EnqueuedJobs("default", 0, int.MaxValue)
                .Count(j => j.Value.Job.Method.Name == "EnviarEmailPedidoEntregadoAsync");
            var antes = avisosDeEntrega();

            await sut.PedidoEntregadoAsync(pedido.PedidoId);
            Assert.Equal(antes + 1, avisosDeEntrega()); // el ganador del claim encola

            // El doble click pierde el claim: corta amable y no repite el aviso
            await Assert.ThrowsAsync<PedidoNoCancelableException>(
                () => sut.PedidoEntregadoAsync(pedido.PedidoId));
            Assert.Equal(antes + 1, avisosDeEntrega());
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
            var sut = Sut.EstadosPedido(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(() => sut.PedidoCanceladoAsync(pedido.PedidoId));
        }

        [Fact]
        public async Task PedidoCancelado_DobleCancelacion_DevuelveElStockUnaSolaVez()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 3,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado);
            var sut = Sut.EstadosPedido(ctx);

            await sut.PedidoCanceladoAsync(pedido.PedidoId);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(
                () => sut.PedidoCanceladoAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(13, stock); // 10 + 3 devueltos una sola vez (no 16)

            var movimientos = await verify.MovimientosStock
                .Where(m => m.PedidoId == pedido.PedidoId && m.Tipo == TipoMovimiento.CancelacionPedido)
                .ToListAsync();
            Assert.Single(movimientos);
        }
    }
}
