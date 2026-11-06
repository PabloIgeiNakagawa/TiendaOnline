using System.Net;
using Moq;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Rutas del front de pedidos/checkout contra la app real: la policy
    /// EsDuenioDelPedido con su handler real (consulta SQLite) y los redirects
    /// del checkout vacío y con stock agotado. Cubre las rutas que movió el
    /// split del controller (el carrito vive en /Carrito, ver CarritosController).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class PedidosFrontendIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public PedidosFrontendIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Detalles_PedidoAjeno_Retorna403()
        {
            // Arrange: el pedido pertenece a "dueno"; el request va autenticado como "ajeno".
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-pedido@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, dueno, producto, 1);
                var ajeno = TestDatabase.SeedUsuario(ctx, "ajeno-pedido@test.local");
                return (PedidoId: pedido.PedidoId, AjenoId: ajeno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.AjenoId);

            // Act
            var response = await client.GetAsync($"/Pedidos/Detalles/{ids.PedidoId}");

            // Assert: autenticado pero no dueño → 403 (el handler real guardó la consulta).
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Checkout_SinCarrito_RedirigeAlCarrito()
        {
            // Arrange: carrito vacío (default del mock de TestApp tras el reset).
            _app.ResetMocks();
            _app.Carrito.Setup(x => x.ValidarStockAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ValidacionStockDto>.Success(new ValidacionStockDto { TodoOK = true }));
            var client = _app.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/Checkout/CheckOut");

            // Assert: carrito vacío → back al carrito con el mensaje listado.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Carrito", response.Headers.Location?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Checkout_CarritoConStockAgotado_RedirigeAlCarrito()
        {
            // Arrange: ValidarStock refrescó contra DB y encontró un item sin unidades.
            _app.ResetMocks();
            _app.Carrito.Setup(x => x.ValidarStockAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ValidacionStockDto>.Success(new ValidacionStockDto
                {
                    TodoOK = false,
                    ItemsSinStock = { new StockInsuficienteDto { ProductoId = 1, Nombre = "Prod", CantidadSolicitada = 2, StockDisponible = 0 } }
                }));
            var client = _app.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/Checkout/CheckOut");

            // Assert: sin avanzar al checkout.
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith("/Carrito", response.Headers.Location?.ToString() ?? string.Empty);
        }
    }
}
