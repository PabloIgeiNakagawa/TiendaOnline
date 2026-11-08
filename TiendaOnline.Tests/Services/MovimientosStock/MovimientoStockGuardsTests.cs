using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.MovimientosStock;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.MovimientosStock
{
    /// <summary>
    /// Guards del ABM manual de stock: las cantidades son siempre positivas, el
    /// inventario no queda en negativo y la devolución solo suma lo no devuelto
    /// del pedido (descontando devoluciones anteriores) de productos que el
    /// pedido realmente compró, sobre pedidos Enviado/Entregado (antes de eso
    /// se anula con la cancelación, que ya devuelve el stock). Si el guard
    /// rechaza, el stock del producto queda como estaba y no se registra
    /// movimiento.
    /// </summary>
    public class MovimientoStockGuardsTests
    {
        private static MovimientoStockCommandService Servicio(TiendaContext ctx)
            => new(ctx, NullLogger<MovimientoStockCommandService>.Instance);

        [Fact]
        public async Task RegistrarEntradaAsync_CantidadNoPositiva_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 10);

            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarEntradaAsync(new RegistroStockDto
                {
                    ProductoId = producto.ProductoId,
                    Cantidad = 0
                }));

            await AssertStockIntacto(ctx, producto.ProductoId, 10);
        }

        [Fact]
        public async Task RegistrarAjusteManualAsync_AjusteDejaStockNegativo_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 3);

            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarAjusteManualAsync(new AjusteManualDto
                {
                    ProductoId = producto.ProductoId,
                    Cantidad = -5,
                    Observaciones = "Baja por rotura en depósito"
                }));

            await AssertStockIntacto(ctx, producto.ProductoId, 3);
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_CantidadNoPositiva_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 4);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);

            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = producto.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 0,
                    Observaciones = "Cliente devolvió el paquete"
                }));

            await AssertStockIntacto(ctx, producto.ProductoId, 4);
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_MasQueLoComprado_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 3);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);

            // El pedido solo compró 2 unidades: devolver 5 inventaría stock que
            // nunca entró al sistema.
            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = producto.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 5,
                    Observaciones = "Cliente devolvió el paquete"
                }));

            await AssertStockIntacto(ctx, producto.ProductoId, 3);
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_DevolucionParcial_SumaStockYRegistraMovimiento()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 3);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, estado: EstadoPedido.Enviado);

            await Servicio(ctx).RegistrarDevolucionAsync(new DevolucionStockDto
            {
                ProductoId = producto.ProductoId,
                PedidoId = pedido.PedidoId,
                Cantidad = 1,
                Observaciones = "El cliente devolvió una de las dos"
            });

            var stock = (await ctx.Productos.AsNoTracking().FirstAsync(p => p.ProductoId == producto.ProductoId)).Stock;
            Assert.Equal(4, stock);

            var movimiento = await ctx.MovimientosStock.AsNoTracking()
                .SingleAsync(m => m.ProductoId == producto.ProductoId);
            Assert.Equal(TipoMovimiento.Devolucion, movimiento.Tipo);
            Assert.Equal(pedido.PedidoId, movimiento.PedidoId);
            Assert.Equal(1, movimiento.Cantidad);
            Assert.StartsWith("Devolución:", movimiento.Observaciones);
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_AcumuladoPasaDeLoComprado_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 5);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, estado: EstadoPedido.Enviado);
            var servicio = Servicio(ctx);

            // Primera devolución válida: 1 de las 2 compradas.
            await servicio.RegistrarDevolucionAsync(new DevolucionStockDto
            {
                ProductoId = producto.ProductoId,
                PedidoId = pedido.PedidoId,
                Cantidad = 1,
                Observaciones = "El cliente devolvió una"
            });

            // Devolver 2 más (resta 1) inventaría unidades que nunca se vendieron: rechaza.
            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => servicio.RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = producto.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 2,
                    Observaciones = "Intento de sobre-devolución"
                }));

            // La siguiente válida consume el restante...
            await servicio.RegistrarDevolucionAsync(new DevolucionStockDto
            {
                ProductoId = producto.ProductoId,
                PedidoId = pedido.PedidoId,
                Cantidad = 1,
                Observaciones = "Segunda devolución del mismo producto"
            });

            // ...y un intento más explota por producto ya devuelto completo.
            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => servicio.RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = producto.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 1,
                    Observaciones = "Segundo intento de sobre-devolución"
                }));

            var stock = (await ctx.Productos.AsNoTracking().FirstAsync(p => p.ProductoId == producto.ProductoId)).Stock;
            Assert.Equal(7, stock);

            Assert.Equal(2, await ctx.MovimientosStock.AsNoTracking()
                .CountAsync(m => m.ProductoId == producto.ProductoId && m.Tipo == TipoMovimiento.Devolucion));
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_PedidoNuevo_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 4);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2);

            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = producto.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 1,
                    Observaciones = "Los estados previos se anulan con la cancelación"
                }));

            await AssertStockIntacto(ctx, producto.ProductoId, 4);
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_PedidoCancelado_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 4);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, estado: EstadoPedido.Cancelado);

            // La cancelación ya devolvió el stock por su propio camino: otra
            // devolución duplicaría unidades.
            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = producto.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 1,
                    Observaciones = "El cliente devolvió el paquete"
                }));

            await AssertStockIntacto(ctx, producto.ProductoId, 4);
        }

        [Fact]
        public async Task RegistrarDevolucionAsync_ProductoAjenoAlPedido_LanzaYDejaStockIntacto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, stock: 4);
            var productoAjeno = TestDatabase.SeedProducto(ctx, stock: 5);
            var usuario = TestDatabase.SeedUsuario(ctx);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, estado: EstadoPedido.Enviado);

            await Assert.ThrowsAsync<MovimientoStockNoValidoException>(
                () => Servicio(ctx).RegistrarDevolucionAsync(new DevolucionStockDto
                {
                    ProductoId = productoAjeno.ProductoId,
                    PedidoId = pedido.PedidoId,
                    Cantidad = 1,
                    Observaciones = "El cliente devolvió el paquete"
                }));

            await AssertStockIntacto(ctx, productoAjeno.ProductoId, 5);
        }

        private static async Task AssertStockIntacto(TiendaContext ctx, int productoId, int stockEsperado)
        {
            var producto = await ctx.Productos.AsNoTracking().FirstAsync(p => p.ProductoId == productoId);
            Assert.Equal(stockEsperado, producto.Stock);
            Assert.Equal(0, await ctx.MovimientosStock.AsNoTracking().CountAsync(m => m.ProductoId == productoId));
        }
    }
}
