using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El POST real de FinalizarCompra contra la app: antiforgery (token leído del
    /// form de /Checkout + cookie del client), binding del ViewModel del wizard
    /// (entrega + pago en un solo POST), recotización del envío contra ZonasEnvio
    /// (el POST jamás define dinero), dirección guardada leída de la BD con
    /// ownership server-side y destinos de redirect. La creación del pedido queda
    /// moqueada: su lógica tiene cobertura unitaria; acá se prueba el cauce completo
    /// del checkout.
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
                ["MetodoPagoId"] = metodoPagoId
            };

        private static Direccion SembrarDireccion(TiendaContext ctx, int usuarioId, string etiqueta = "Casa",
            string calle = "San Martin", string numero = "123", string localidad = "Cordoba",
            string provincia = "Córdoba", string codigoPostal = "5000")
        {
            var direccion = new Direccion
            {
                UsuarioId = usuarioId,
                Etiqueta = etiqueta,
                Calle = calle,
                Numero = numero,
                Localidad = localidad,
                Provincia = provincia,
                CodigoPostal = codigoPostal,
                Activo = true
            };
            ctx.Direcciones.Add(direccion);
            ctx.SaveChanges();
            return direccion;
        }

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

            // SubTotal y CostoEnvio "envenenados": display del wizard que el binding
            // acepta pero el server nunca usa (el dinero se recalcula de siempre).
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
        public async Task FinalizarCompra_EnvioDomicilio_NuevaDireccion_RecotizaElCostoDesdeZonas()
        {
            // Arrange: las 24 zonas ya están sembradas por el boot idempotente de Program
            // (DbInitializer.SeedZonasEnvio), así que tiramos del precio real de "Misiones"
            // y probamos el match case-insensitive de la provincia del form ("misiones").
            _app.ResetMocks();
            var usuario = _app.WithDb(ctx => TestDatabase.SeedUsuario(ctx, "checkout-envio@test.local"));
            var costoMisiones = _app.WithDb(ctx =>
                ctx.ZonasEnvio.AsNoTracking().Single(z => z.Provincia == "Misiones").Costo);
            SetupCarritoValido(productoId: 7, precio: 250m, cantidad: 1);
            SetupCrearPedido(pedidoIdDevuelto: 98);

            var client = _app.CreateClientAs(usuario.UsuarioId);
            var token = await IrAlCheckoutYTomarTokenAsync(client);

            var form = FormBase(token, "1", "2"); // EnvioDomicilio, Transferencia
            form["CostoEnvio"] = "999"; // envenenado: se re-cotiza contra la zona, siempre
            form["NuevaDireccion.Etiqueta"] = "Cabaña";
            form["NuevaDireccion.Provincia"] = "misiones";
            form["NuevaDireccion.Localidad"] = "Posadas";
            form["NuevaDireccion.CodigoPostal"] = "3300";
            form["NuevaDireccion.Calle"] = "Av. Siempre Viva";
            form["NuevaDireccion.Numero"] = "742";

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

            // La dirección nueva queda guardada en el perfil del usuario (service real).
            var guardadaEnPerfil = _app.WithDb(ctx => ctx.Direcciones.AsNoTracking()
                .Any(d => d.UsuarioId == usuario.UsuarioId && d.Etiqueta == "Cabaña"));
            Assert.True(guardadaEnPerfil);
        }

        [Fact]
        public async Task FinalizarCompra_EnvioDomicilio_DireccionGuardada_TomaLosDatosDeLaBase()
        {
            // Arrange: la dirección guardada viaja SOLO como id — ni provincia ni calle
            // ni dinero. El server la lee del usuario (ownership) y la provincia de la
            // BD es la que dispara la re-cotización.
            _app.ResetMocks();
            var usuario = _app.WithDb(ctx => TestDatabase.SeedUsuario(ctx, "checkout-guardada@test.local"));
            var direccionId = _app.WithDb(ctx => SembrarDireccion(ctx, usuario.UsuarioId,
                calle: "Bv. Los Andes", numero: "987", provincia: "Córdoba").DireccionId);
            var costoCordoba = _app.WithDb(ctx =>
                ctx.ZonasEnvio.AsNoTracking().Single(z => z.Provincia == "Córdoba").Costo);
            SetupCarritoValido();
            SetupCrearPedido(pedidoIdDevuelto: 101);

            var client = _app.CreateClientAs(usuario.UsuarioId);
            var token = await IrAlCheckoutYTomarTokenAsync(client);

            // Act: el POST no manda ningún dato de la dirección (tampoco envenenados):
            // solo el id y el método de pago.
            var form = FormBase(token, "1", "2"); // EnvioDomicilio, Transferencia
            form["DireccionSeleccionadaId"] = direccionId.ToString();
            var response = await client.PostAsync("/Checkout/FinalizarCompra", new FormUrlEncodedContent(form));

            // Assert: todos los datos de envío vienen de la BD.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Pedidos/Instrucciones", response.Headers.Location?.ToString() ?? string.Empty);

            Assert.NotNull(_dtoCapturado);
            Assert.True(_dtoCapturado.EsEnvioADomicilio);
            Assert.Equal(costoCordoba, _dtoCapturado.CostoEnvio);
            Assert.Equal("Córdoba", _dtoCapturado.EnvioProvincia);
            Assert.Equal("Bv. Los Andes", _dtoCapturado.EnvioCalle);
            Assert.Equal("987", _dtoCapturado.EnvioNumero);
            Assert.Equal("5000", _dtoCapturado.EnvioCodigoPostal);

            // La guardada ya vive en el perfil: no se vuelve a guardar (solo queda
            // la sembrada — el service de direcciones corre real contra la DB).
            var totalDirecciones = _app.WithDb(ctx => ctx.Direcciones.AsNoTracking()
                .Count(d => d.UsuarioId == usuario.UsuarioId));
            Assert.Equal(1, totalDirecciones);
        }

        [Fact]
        public async Task FinalizarCompra_DireccionGuardadaAjena_FrenaSinCrearPedido()
        {
            // Arrange: la dirección que viaja en el POST es de OTRO usuario. El id del
            // POST no es autorización: la lectura solo alcanza las del usuario logueado.
            _app.ResetMocks();
            var duenio = _app.WithDb(ctx => TestDatabase.SeedUsuario(ctx, "checkout-ajena-duenio@test.local"));
            var atacante = _app.WithDb(ctx => TestDatabase.SeedUsuario(ctx, "checkout-ajena-ataca@test.local"));
            var direccionAjenaId = _app.WithDb(ctx => SembrarDireccion(ctx, duenio.UsuarioId).DireccionId);
            SetupCarritoValido();
            SetupCrearPedido(pedidoIdDevuelto: 102);

            var client = _app.CreateClientAs(atacante.UsuarioId);
            var token = await IrAlCheckoutYTomarTokenAsync(client);

            var form = FormBase(token, "1", "2");
            form["DireccionSeleccionadaId"] = direccionAjenaId.ToString();

            // Act
            var response = await client.PostAsync("/Checkout/FinalizarCompra", new FormUrlEncodedContent(form));

            // Assert: de vuelta al checkout con el aviso amable; jamás se crea el pedido.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Checkout/CheckOut", response.Headers.Location?.ToString() ?? string.Empty);
            _app.CreacionPedido.Verify(
                x => x.CrearPedidoYPrepararPagoAsync(It.IsAny<CrearPedidoDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
