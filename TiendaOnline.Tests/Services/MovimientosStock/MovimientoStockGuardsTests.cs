using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.MovimientosStock;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.MovimientosStock
{
    /// <summary>
    /// Guards del ABM manual de stock: las cantidades son siempre positivas, el
    /// inventario no queda en negativo y la devolución solo suma hasta lo
    /// comprado en el pedido. Si el guard rechaza, el stock del producto queda
    /// como estaba y el movimiento no se registra.
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

        private static async Task AssertStockIntacto(TiendaContext ctx, int productoId, int stockEsperado)
        {
            var producto = await ctx.Productos.AsNoTracking().FirstAsync(p => p.ProductoId == productoId);
            Assert.Equal(stockEsperado, producto.Stock);
            Assert.Equal(0, await ctx.MovimientosStock.AsNoTracking().CountAsync(m => m.ProductoId == productoId));
        }
    }
}
