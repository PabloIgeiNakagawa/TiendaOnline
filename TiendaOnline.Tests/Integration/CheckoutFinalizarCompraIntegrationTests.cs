using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El POST real de FinalizarCompra contra la app: antiforgery (token leído del
    /// form de /Checkout + cookie del client), binding del ViewModel, recotización
    /// del envío contra ZonasEnvio (el POST jamás define dinero) y destinos de
    /// redirect. La creación del pedido queda moqueada: su lógica tiene cobertura
    /// unitaria; acá se prueba el cauce completo del checkout.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class CheckoutFinalizarCompraIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public CheckoutFinalizarCompraIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static string ExtraerTokenAntiforgery(string html)
        {
            var coincidencia = Regex.Match(
                html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

            Assert.True(coincidencia.Success, "El formulario no renderizó el token de antiforgery.");
            return coincidencia.Groups[1].Value;
        }

        // GET de la página del checkout: entrega la cookie de antiforgery (queda en el
        // cookie jar del client) y el token embebido en el form. Ambos viajan juntos
        // al POST siguiente, como en el browser.
        private async Task<string> IrAlCheckoutYTomarTokenAsync(HttpClient client)
        {
            var response = await client.GetAsync("/Checkout/CheckOut");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return ExtraerTokenAntiforgery(await response.Content.ReadAsStringAsync());
        }

        private void SetupCarritoValido(int productoId = 5, decimal precio = 100m, int cantidad = 2)
        {
            _app.Carrito.Setup(x => x.ValidarStockAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ValidacionStockDto>.Success(new ValidacionStockDto { TodoOK = true }));

            // FinalizarCompra lee el carrito dos veces (antes y después de validar):
            // misma lista en ambas lecturas.
            _app.Carrito.Setup(x => x.ObtenerAsync())
                .ReturnsAsync(new List<ItemCarrito>
                {
                    new ItemCarrito
                    {
                        ProductoId = productoId,
                        Nombre = "Producto Test",
                        Precio = precio,
                        PrecioLista = precio,
                        Cantidad = cantidad,
                        ImagenUrl = "producto.png"
                    }
                });
        }

        private CrearPedidoDto? _dtoCapturado;

        private void SetupCrearPedido(int pedidoIdDevuelto)
        {
            _app.CreacionPedido
                .Setup(x => x.CrearPedidoYPrepararPagoAsync(It.IsAny<CrearPedidoDto>(), It.IsAny<CancellationToken>()))
                .Callback<CrearPedidoDto, CancellationToken>((dto, _) => _dtoCapturado = dto)
                .ReturnsAsync(new PedidoPagoDto { PedidoId = pedidoIdDevuelto });
        }

        private static Dictionary<string, string> FormBase(string token, string metodoEntrega, string metodoPagoId)
            => new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["MetodoEntrega"] = metodoEntrega,
                ["MetodoDePagoId"] = metodoPagoId
            };

        [Fact]
        public async Task FinalizarCompra_RetiroLocalTransferencia_CreaPedidoYVaciaElCarrito()
        {
            // Arrange: usuario real en la DB, pedido moqueado, carrito válido.
            _app.ResetMocks();
            var usuario = _app.WithDb(ctx => TestDatabase.SeedUsuario(ctx, "checkout-retiro@test.local"));
            SetupCarritoValido(productoId: 5, precio: 100m, cantidad: 2);
            SetupCrearPedido(pedidoIdDevuelto: 97);

            var client = _app.CreateClientAs(usuario.UsuarioId);
            var token = await IrAlCheckoutYTomarTokenAsync(client);

            // SubTotal y CostoEnvio "envenenados": el POST no puede definir dinero.
            var form = FormBase(token, "0", "2"); // RetiroLocal, Transferencia
            form["SubTotal"] = "123.45";
            form["CostoEnvio"] = "999";

            // Act
            var response = await client.PostAsync("/Checkout/FinalizarCompra", new FormUrlEncodedContent(form));

            // Assert: pedido creado con los datos del server (no los envenenados).
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Pedidos/Instrucciones", response.Headers.Location?.ToString() ?? string.Empty);

            Assert.NotNull(_dtoCapturado);
            Assert.Equal(usuario.UsuarioId, _dtoCapturado.UsuarioId);
            Assert.Equal(2, _dtoCapturado.MetodoDePagoId);
            Assert.False(_dtoCapturado.EsEnvioADomicilio);
            Assert.Equal(0m, _dtoCapturado.CostoEnvio); // retiro = $0, no el 999 del form

            var item = Assert.Single(_dtoCapturado.Items);
            Assert.Equal(5, item.ProductoId);
            Assert.Equal(2, item.Cantidad);
            Assert.Equal(100m, item.PrecioUnitario); // del carrito refrescado, no del POST

            // Transferencia: el carrito se vacía recién cuando el pedido quedó armado,
            // y por Mercado Pago no pasa nada (esa ruta es del test de preferencia).
            _app.Carrito.Verify(x => x.VaciarAsync(), Times.Once);
            _app.Payment.Verify(
                x => x.GenerarPreferenciaPagoAsync(It.IsAny<PedidoPagoDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task FinalizarCompra_EnvioDomicilio_RecotizaElCostoDesdeZonas()
        {
            // Arrange: las 24 zonas ya están sembradas por el boot idempotente de Program
            // (DbInitializer.SeedZonasEnvio), así que tiramos del precio real de "Misiones"
            // y probamos el match case-insensitive del POST ("misiones" minúscula).
            _app.ResetMocks();
            var usuario = _app.WithDb(ctx =>
            {
                var usuario = TestDatabase.SeedUsuario(ctx, "checkout-envio@test.local");
                return usuario;
            });
            var costoMisiones = _app.WithDb(ctx =>
                ctx.ZonasEnvio.AsNoTracking().Single(z => z.Provincia == "Misiones").Costo);
            SetupCarritoValido(productoId: 7, precio: 250m, cantidad: 1);
            SetupCrearPedido(pedidoIdDevuelto: 98);

            var client = _app.CreateClientAs(usuario.UsuarioId);
            var token = await IrAlCheckoutYTomarTokenAsync(client);

            var form = FormBase(token, "1", "2"); // EnvioDomicilio, Transferencia
            form["CostoEnvio"] = "999"; // envenenado: se re-cotiza contra la zona, siempre
            form["Direccion.EsNueva"] = "false";
            form["Direccion.Provincia"] = "misiones";
            form["Direccion.Localidad"] = "Posadas";
            form["Direccion.CodigoPostal"] = "3300";
            form["Direccion.Calle"] = "Av. Siempre Viva";
            form["Direccion.Numero"] = "742";

            // Act
            var response = await client.PostAsync("/Checkout/FinalizarCompra", new FormUrlEncodedContent(form));

            // Assert: el costo viene de la tabla, no del POST ni del carrito.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Pedidos/Instrucciones", response.Headers.Location?.ToString() ?? string.Empty);

            Assert.NotNull(_dtoCapturado);
            Assert.True(_dtoCapturado.EsEnvioADomicilio);
            Assert.Equal(costoMisiones, _dtoCapturado.CostoEnvio);
            Assert.Equal("misiones", _dtoCapturado.EnvioProvincia);
            Assert.Equal("Av. Siempre Viva", _dtoCapturado.EnvioCalle);
            Assert.Equal("742", _dtoCapturado.EnvioNumero);
        }

        [Fact]
        public async Task FinalizarCompra_EnMercadoPago_RedirigeALaPreferencia()
        {
            // Arrange
            _app.ResetMocks();
            var usuario = _app.WithDb(ctx => TestDatabase.SeedUsuario(ctx, "checkout-mp@test.local"));
            SetupCarritoValido();
            SetupCrearPedido(pedidoIdDevuelto: 99);

            const string urlPreferencia = "https://sandbox.mercadopago.com.ar/preferencia-test";
            _app.Payment
                .Setup(x => x.GenerarPreferenciaPagoAsync(It.IsAny<PedidoPagoDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(urlPreferencia);

            var client = _app.CreateClientAs(usuario.UsuarioId);
            var token = await IrAlCheckoutYTomarTokenAsync(client);

            // Act: Mercado Pago = sale de la tienda hacia el hosted gateway.
            var form = FormBase(token, "0", "1"); // RetiroLocal, MercadoPago
            var response = await client.PostAsync("/Checkout/FinalizarCompra", new FormUrlEncodedContent(form));

            // Assert: redirect absoluto a la preferencia; el carrito NO se vacía acá
            // (eso ocurre al confirmar el pago, vía webhook/pago exitoso).
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(urlPreferencia, response.Headers.Location?.ToString());

            _app.Carrito.Verify(x => x.VaciarAsync(), Times.Never);
        }

        [Fact]
        public async Task FinalizarCompra_SinTokenAntiforgery_NoCreaPedido()
        {
            // Arrange: POST directo con form válido pero sin __RequestVerificationToken
            // (tampoco cookie de antiforgery: el client nunca visitó el checkout).
            _app.ResetMocks();
            SetupCarritoValido();
            SetupCrearPedido(pedidoIdDevuelto: 100);

            var client = _app.CreateClientAs(4242);

            var form = FormBase("", "0", "2"); // Sin token
            form["__RequestVerificationToken"] = ""; // explícito vacío

            // Act
            var response = await client.PostAsync("/Checkout/FinalizarCompra", new FormUrlEncodedContent(form));

            // Assert: el filtro de antiforgery corta antes de tocar nada (400) y el
            // pedido jamás se crea ni el carrito se toca.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _app.CreacionPedido.Verify(
                x => x.CrearPedidoYPrepararPagoAsync(It.IsAny<CrearPedidoDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _app.Carrito.Verify(x => x.VaciarAsync(), Times.Never);
        }
    }
}
