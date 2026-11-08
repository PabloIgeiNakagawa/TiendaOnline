using Microsoft.Playwright;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using static Microsoft.Playwright.Assertions;

namespace TiendaOnline.E2E
{
    /// <summary>
    /// Smoke test 3: el circuito de devolución más nuevo del panel. Un pedido Enviado
    /// (sembrado directo a la base) entra por /Admin/MovimientosStock/Devolucion,
    /// el admin registra 2 de las 3 unidades y la base muestra el stock que vuelve
    /// y el movimiento con la referencia del pedido. El guard del "ya devuelto"
    /// queda cubierto por la suite unitaria de MovimientosStock.
    /// </summary>
    [Collection("E2E")]
    public class Smoke3_DevolucionDelAdmin
    {
        private readonly E2EFixture _fixture;

        public Smoke3_DevolucionDelAdmin(E2EFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Devolucion_RegistradaPorElAdmin_DevuelveStockYQuedaTraza()
        {
            // Arranque: pedido Enviado con 3 unidades (stock 5 del seed del producto).
            var nombreProducto = "Router Devolución E2E";
            var pedidoId = _fixture.ConDb(ctx =>
            {
                var producto = new Producto
                {
                    Nombre = nombreProducto,
                    Descripcion = "Producto solo del smoke test 3",
                    Precio = 1200m,
                    Stock = 5,
                    ImagenUrl = "https://img.e2e.test/devolucion.png",
                    CategoriaId = ctx.Categorias.First().CategoriaId
                };
                ctx.Productos.Add(producto);

                var usuario = ctx.Usuarios.First(u => u.Email == E2EFixture.EmailClienteSeed);
                var pago = ctx.Set<MetodoDePago>().First();
                var pedido = new Pedido
                {
                    UsuarioId = usuario.UsuarioId,
                    MetodoDePagoId = pago.MetodoDePagoId,
                    EsEnvioADomicilio = false,
                    Estado = EstadoPedido.Enviado,
                    EstadoPago = EstadoPago.Aprobado,
                    FechaPedido = DateTime.Now,
                    FechaEnPreparacion = DateTime.Now,
                    FechaEnvio = DateTime.Now
                };
                pedido.DetallesPedido.Add(new DetallePedido
                {
                    ProductoId = producto.ProductoId,
                    Cantidad = 3,
                    PrecioUnitario = producto.Precio,
                    Producto = producto
                });
                ctx.Pedidos.Add(pedido);
                ctx.SaveChanges();

                return pedido.PedidoId;
            });

            var contexto = await _fixture.CrearContextoAsync();
            var page = await contexto.NewPageAsync();

            await CapturasDeFalla.SiFallaAsync(page, "smoke3", async () =>
            {
                await _fixture.LoginAsync(page, E2EFixture.EmailAdmin);
                // El post-login del Administrador va a /admin (ruta literal de IndexAdmin).
                await page.WaitForURLAsync("**/admin");

                await page.GotoAsync(
                    $"{_fixture.BaseUrl}/Admin/MovimientosStock/Devolucion?idPedido={pedidoId}");

                var item = page.Locator("div.border.rounded-3.p-3.mb-3", new PageLocatorOptions { HasText = nombreProducto });
                await Expect(item).ToContainTextAsync("Compradas: 3");

                await item.Locator("input[name=\"Cantidad\"]").FillAsync("2");
                await item.Locator("input[name=\"Observaciones\"]").FillAsync("El cliente lo devolvió sin abrir");
                await item.Locator("button:has-text(\"Registrar\")").ClickAsync();

                // La misma pantalla muestra el acumulado actualizado y el tope restante
                await Expect(item).ToContainTextAsync("Ya devueltas: 2");
                await Expect(item).ToContainTextAsync("Quedan por devolver: 1");
                await Expect(page.GetByText("Devolución registrada")).ToBeVisibleAsync();
            });

            await contexto.DisposeAsync();

            // Efectos en la base: el stock volvió y quedó la traza del movimiento.
            var stock = _fixture.ConDb(ctx => ctx.Productos
                .AsNoTracking()
                .Single(p => p.Nombre == nombreProducto).Stock);
            Assert.Equal(7, stock);

            var movimiento = _fixture.ConDb(ctx => ctx.MovimientosStock
                .AsNoTracking()
                .Single(m => m.Tipo == TipoMovimiento.Devolucion));
            Assert.Equal(2, movimiento.Cantidad);
            Assert.Equal(pedidoId, movimiento.PedidoId);
            Assert.Contains("Devolución:", movimiento.Observaciones);
        }
    }
}
