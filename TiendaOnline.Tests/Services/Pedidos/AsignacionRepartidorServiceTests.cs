using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Asignación del reparto: la toma del repartidor y la asignación/reasignación
    /// del admin son claims condicionales. Los tests fijan los contratos de acción:
    /// un pedido tomado por otro no se pisa, un pedido fuera de reparto no se toma,
    /// y el admin puede asignar en preparación o en camino.
    /// </summary>
    public class AsignacionRepartidorServiceTests
    {
        private static (TestDatabase Db, int PedidoId, int RepartidorId, int OtroRepartidorId) Sembrar(
            EstadoPedido estado, int? repartidorId = null)
        {
            var db = new TestDatabase();
            using var ctx = db.CreateContext();
            var cliente = TestDatabase.SeedUsuario(ctx);
            var repartidor = TestDatabase.SeedUsuario(ctx, rol: Rol.Repartidor);
            var otro = TestDatabase.SeedUsuario(ctx, rol: Rol.Repartidor);
            var producto = TestDatabase.SeedProducto(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, cliente, producto, 1,
                estado: estado,
                repartidorId: repartidorId == -1 ? repartidor.UsuarioId : repartidorId);

            return (db, pedido.PedidoId, repartidor.UsuarioId, otro.UsuarioId);
        }

        [Fact]
        public async Task TomarPedido_Libre_AsignaAlRepartidor()
        {
            var s = Sembrar(EstadoPedido.Enviado);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, s.RepartidorId);

            await sut.TomarPedidoAsync(s.PedidoId);

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Equal(s.RepartidorId, pedido.RepartidorId);
        }

        [Fact]
        public async Task TomarPedido_YaTomadoPorOtro_LanzaYNoPisaAlDuenio()
        {
            var s = Sembrar(EstadoPedido.Enviado, repartidorId: -1); // el primer repartidor lo tiene
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, s.OtroRepartidorId);

            await Assert.ThrowsAsync<PedidoNoAsignableException>(() => sut.TomarPedidoAsync(s.PedidoId));

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Equal(s.RepartidorId, pedido.RepartidorId);
        }

        [Fact]
        public async Task TomarPedido_FueraDeReparto_Lanza()
        {
            var s = Sembrar(EstadoPedido.EnPreparacion); // todavía no salió
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, s.RepartidorId);

            await Assert.ThrowsAsync<PedidoNoAsignableException>(() => sut.TomarPedidoAsync(s.PedidoId));
        }

        [Fact]
        public async Task TomarPedido_DobleClickDelMismo_NoLanza()
        {
            var s = Sembrar(EstadoPedido.Enviado);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, s.RepartidorId);

            await sut.TomarPedidoAsync(s.PedidoId);
            await sut.TomarPedidoAsync(s.PedidoId); // idempotente para el dueño

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Equal(s.RepartidorId, pedido.RepartidorId);
        }

        [Fact]
        public async Task LiberarPedido_Propio_LoDejaSinAsignar()
        {
            var s = Sembrar(EstadoPedido.Enviado, repartidorId: -1);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, s.RepartidorId);

            await sut.LiberarPedidoAsync(s.PedidoId);

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Null(pedido.RepartidorId);
        }

        [Fact]
        public async Task LiberarPedido_Ajeno_LanzaYNoLoSuelta()
        {
            var s = Sembrar(EstadoPedido.Enviado, repartidorId: -1);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, s.OtroRepartidorId);

            await Assert.ThrowsAsync<PedidoNoAsignableException>(() => sut.LiberarPedidoAsync(s.PedidoId));

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Equal(s.RepartidorId, pedido.RepartidorId);
        }

        [Fact]
        public async Task AsignarRepartidor_AdminEnPreparacion_Asigna()
        {
            var s = Sembrar(EstadoPedido.EnPreparacion);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, 0); // admin: el id de sesión no decide la asignación

            await sut.AsignarRepartidorAsync(s.PedidoId, s.OtroRepartidorId);

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Equal(s.OtroRepartidorId, pedido.RepartidorId);
        }

        [Fact]
        public async Task AsignarRepartidor_SinRepartidor_LoDejaLibre()
        {
            var s = Sembrar(EstadoPedido.Enviado, repartidorId: -1);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, 0);

            await sut.AsignarRepartidorAsync(s.PedidoId, null);

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Null(pedido.RepartidorId);
        }

        [Fact]
        public async Task AsignarRepartidor_UsuarioComun_LanzaYNoAsigna()
        {
            var s = Sembrar(EstadoPedido.EnPreparacion);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var comun = TestDatabase.SeedUsuario(ctx);
            var sut = Sut.AsignacionRepartidor(ctx, 0);

            await Assert.ThrowsAsync<PedidoNoAsignableException>(
                () => sut.AsignarRepartidorAsync(s.PedidoId, comun.UsuarioId));

            using var verify = db.CreateContext();
            var pedido = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == s.PedidoId);
            Assert.Null(pedido.RepartidorId);
        }

        [Fact]
        public async Task AsignarRepartidor_EnNuevo_Lanza()
        {
            var s = Sembrar(EstadoPedido.Nuevo);
            using var db = s.Db;
            await using var ctx = db.CreateContext();
            var sut = Sut.AsignacionRepartidor(ctx, 0);

            await Assert.ThrowsAsync<PedidoNoAsignableException>(
                () => sut.AsignarRepartidorAsync(s.PedidoId, s.RepartidorId));
        }
    }
}
