using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Services.Ofertas;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Tests de CrearPedidoYPrepararPagoAsync: consolidación de items,
    /// reglas de precio (recalcular en servidor no confiar en el cliente) y rollback.
    /// </summary>
    public class CreacionPedidoServiceTests
    {
        private static CrearPedidoDto Dto(int usuarioId, int metodoPagoId, params CrearPedidoDetalleDto[] items)
            => new() { UsuarioId = usuarioId, MetodoDePagoId = metodoPagoId, Items = items.ToList() };

        [Fact]
        public async Task CrearPedido_SinItems_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = Sut.CreacionPedido(ctx);

            await Assert.ThrowsAsync<Exception>(
                () => sut.CrearPedidoYPrepararPagoAsync(Dto(1, 1)));
        }

        [Fact]
        public async Task CrearPedido_ProductoInexistente_LanzaExcepcion_YNoPersiste()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var sut = Sut.CreacionPedido(ctx);

            await Assert.ThrowsAsync<Exception>(
                () => sut.CrearPedidoYPrepararPagoAsync(Dto(
                    usuario.UsuarioId, 1,
                    new CrearPedidoDetalleDto { ProductoId = 99999, Cantidad = 1, PrecioUnitario = 10m })));

            using var verify = db.CreateContext();
            Assert.Equal(0, await verify.Pedidos.CountAsync());
        }

        [Fact]
        public async Task CrearPedido_ItemsDuplicados_AgrupaPorProducto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var metodo = TestDatabase.SeedMetodoDePago(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            var sut = Sut.CreacionPedido(ctx);

            var pago = await sut.CrearPedidoYPrepararPagoAsync(Dto(
                usuario.UsuarioId, metodo.MetodoDePagoId,
                new CrearPedidoDetalleDto { ProductoId = producto.ProductoId, Cantidad = 1, PrecioUnitario = 100m },
                new CrearPedidoDetalleDto { ProductoId = producto.ProductoId, Cantidad = 2, PrecioUnitario = 100m }));

            Assert.NotEqual(0, pago.PedidoId);

            using var verify = db.CreateContext();
            var detalles = await verify.DetallesPedido
                .Where(dp => dp.PedidoId == pago.PedidoId)
                .ToListAsync();
            var detalleUnico = Assert.Single(detalles);
            Assert.Equal(3, detalleUnico.Cantidad); // 1 + 2 agrupados
        }

        [Fact]
        public async Task CrearPedido_TomaPrecioDesdeBaseDeDatos_NoDelDto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var metodo = TestDatabase.SeedMetodoDePago(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10); // precio real en BD
            var sut = Sut.CreacionPedido(ctx);

            // El cliente envía un precio manipulado (manipulación del carrito)
            var pago = await sut.CrearPedidoYPrepararPagoAsync(Dto(
                usuario.UsuarioId, metodo.MetodoDePagoId,
                new CrearPedidoDetalleDto { ProductoId = producto.ProductoId, Cantidad = 1, PrecioUnitario = 0.01m }));

            using var verify = db.CreateContext();
            var detalle = await verify.DetallesPedido.FirstAsync(dp => dp.PedidoId == pago.PedidoId);
            Assert.Equal(100m, detalle.PrecioUnitario); // el sistema recalcula desde Producto en BD
        }

        [Fact]
        public async Task CrearPedido_ConOfertaVigente_SnapshotConPrecioOferta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var metodo = TestDatabase.SeedMetodoDePago(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 79.9m);
            var sut = Sut.CreacionPedido(ctx, ofertas: new OfertaQueryService(ctx)); // resolver real sobre la DB de test

            var pago = await sut.CrearPedidoYPrepararPagoAsync(Dto(
                usuario.UsuarioId, metodo.MetodoDePagoId,
                new CrearPedidoDetalleDto { ProductoId = producto.ProductoId, Cantidad = 2, PrecioUnitario = 0.01m }));

            using var verify = db.CreateContext();
            var detalle = await verify.DetallesPedido.FirstAsync(dp => dp.PedidoId == pago.PedidoId);
            Assert.Equal(79.9m, detalle.PrecioUnitario); // efectivo = oferta vigente (ni lista ni precio del cliente)
        }

        [Fact]
        public async Task CrearPedido_ConOfertaExpirada_SnapshotConPrecioLista()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var metodo = TestDatabase.SeedMetodoDePago(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 40m,
                inicio: DateTime.Now.AddDays(-5), fin: DateTime.Now.AddDays(-1)); // expirada: no aplica
            var sut = Sut.CreacionPedido(ctx, ofertas: new OfertaQueryService(ctx));

            var pago = await sut.CrearPedidoYPrepararPagoAsync(Dto(
                usuario.UsuarioId, metodo.MetodoDePagoId,
                new CrearPedidoDetalleDto { ProductoId = producto.ProductoId, Cantidad = 1, PrecioUnitario = 0.01m }));

            using var verify = db.CreateContext();
            var detalle = await verify.DetallesPedido.FirstAsync(dp => dp.PedidoId == pago.PedidoId);
            Assert.Equal(100m, detalle.PrecioUnitario); // oferta expirada → precio de lista
        }

        [Fact]
        public async Task CrearPedido_ConEnvioADomicilio_DevuelveElCostoDeEnvioParaLaPreferencia()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx);
            var metodo = TestDatabase.SeedMetodoDePago(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 10);
            var sut = Sut.CreacionPedido(ctx);

            var dto = Dto(usuario.UsuarioId, metodo.MetodoDePagoId,
                new CrearPedidoDetalleDto { ProductoId = producto.ProductoId, Cantidad = 1, PrecioUnitario = 100m });
            dto.EsEnvioADomicilio = true;
            dto.CostoEnvio = 50m;

            var pago = await sut.CrearPedidoYPrepararPagoAsync(dto);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pago.PedidoId);
            Assert.Equal(50m, pedidoDb.CostoEnvio);
            Assert.True(pedidoDb.EsEnvioADomicilio);
            Assert.Equal(50m, pago.CostoEnvio); // el costo viaja a la preferencia para el ítem "Envío"
        }
    }
}
