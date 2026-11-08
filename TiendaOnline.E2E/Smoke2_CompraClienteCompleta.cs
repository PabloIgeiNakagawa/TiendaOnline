using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TiendaOnline.Domain.Entities;
using static Microsoft.Playwright.Assertions;

namespace TiendaOnline.E2E
{
    /// <summary>
    /// Smoke test 2: el caminar completo del cliente. Se registra desde el form, loguea,
    /// agrega el producto al carrito, checkout con retiro local, paga "por
    /// MercadoPago" (el tramo externo es el fake, la vuelta con payment_id es real)
    /// y termina con el pedido Aprobado + stock reservado en la base.
    /// </summary>
    [Collection("E2E")]
    public class Smoke2_CompraClienteCompleta
    {
        private readonly E2EFixture _fixture;

        public Smoke2_CompraClienteCompleta(E2EFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Compra_DeUnCliente_CreaPedidoPagadoYReservaStock()
        {
            // Email propio del run: el registro del smoke test tiene que crear la cuenta de
            // verdad. Formato corto (≤ 50): el RegisterViewModel exige MaxLength(50)
            // y un GUID completo de 32 pasaría esa valla.
            var emailRegistro = $"smoke-{Guid.NewGuid().ToString("N")[..6]}@e2e.local";

            var contexto = await _fixture.CrearContextoAsync();
            var page = await contexto.NewPageAsync();

            await CapturasDeFalla.SiFallaAsync(page, "smoke2", async () =>
            {
                // Registro por el formulario real
                await page.GotoAsync($"{_fixture.BaseUrl}/Accounts/Register");
                await page.FillAsync("#campoNombre", "Cliente");
                await page.FillAsync("#campoApellido", "Smoke");
                await page.FillAsync("#campoFechaNacimiento", "1995-05-05");
                await page.FillAsync("#campoTelefono", "3510002222");
                await page.FillAsync("#campoEmail", emailRegistro);
                await page.FillAsync("#campoContrasena", E2EFixture.Password);
                await page.FillAsync("#campoConfirmarContrasena", E2EFixture.Password);
                await page.CheckAsync("#aceptarTerminos");
                await page.ClickAsync("#botonEnviar");
                await page.WaitForURLAsync("**/Accounts/Login");

                // Login con la cuenta recién creada
                await page.FillAsync("#email", emailRegistro);
                await page.FillAsync("#password", E2EFixture.Password);
                await page.ClickAsync("#loginBtn");
                await page.WaitForURLAsync(new Regex($"^{Regex.Escape(_fixture.BaseUrl)}/$"));

                // Agregar al carrito desde el listado de productos
                await page.GotoAsync($"{_fixture.BaseUrl}/Productos");
                await page.ClickAsync("button:has-text(\"Agregar al carrito\")");
                await page.WaitForURLAsync("**/Carrito/Index");
                // El nombre del producto aparece en varios nodos de la página
                // (título del item y resumen): al primero le alcanza.
                await Expect(page.GetByText(E2EFixture.NombreProductoTienda).First).ToBeVisibleAsync();

                // Checkout: retiro en el local (no exige dirección). El radio de
                // Bootstrap es visualmente oculto: el click lo dispara el label.
                await page.GotoAsync($"{_fixture.BaseUrl}/Checkout/CheckOut");
                await page.ClickAsync("label[for=\"radioLocal\"]");
                await Expect(page.Locator("#btnConfirmarPedido")).ToBeEnabledAsync();
                await page.ClickAsync("#btnConfirmarPedido");
                await page.WaitForURLAsync("**/Checkout/Confirmacion");

                // Pagar: MP falso pre-seleccionado; vuelve con el payment_id aprobado
                await page.ClickAsync("button:has-text(\"Confirmar Compra\")");
                await page.WaitForURLAsync("**/Pedidos/PagoExitoso*");
                await Expect(page.GetByText("¡Pago confirmado exitosamente!")).ToBeVisibleAsync();

                // El pedido queda visible para el cliente
                await page.GotoAsync($"{_fixture.BaseUrl}/Pedidos/MisPedidos");
                await Expect(page.Locator("body")).ToContainTextAsync(E2EFixture.NombreProductoTienda);
            });

            await contexto.DisposeAsync();

            // Efectos en la base: pago aprobado, pedido en preparación, stock
            // reservado y el movimiento SalidaVenta registrado con el pedido.
            var pedido = _fixture.ConDb(ctx => ctx.Pedidos
                .Where(p => p.Usuario!.Email == emailRegistro)
                .Include(p => p.DetallesPedido)
                .AsNoTracking()
                .Single());
            Assert.Equal(EstadoPago.Aprobado, pedido.EstadoPago);
            Assert.Equal(EstadoPedido.EnPreparacion, pedido.Estado);
            Assert.NotEqual(default, pedido.FechaEnPreparacion);

            var stock = _fixture.ConDb(ctx => ctx.Productos
                .AsNoTracking()
                .Single(p => p.Nombre == E2EFixture.NombreProductoTienda).Stock);
            Assert.Equal(4, stock);

            var salida = _fixture.ConDb(ctx => ctx.MovimientosStock
                .AsNoTracking()
                .Single(m => m.Tipo == TipoMovimiento.SalidaVenta && m.PedidoId == pedido.PedidoId));
            Assert.Equal(pedido.PedidoId, salida.PedidoId);
        }
    }
}
