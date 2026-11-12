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

        // Atajo del comprobante en Mis Pedidos: el pedido transferencia que espera
        // su comprobante muestra el chip "Falta comprobante" y el botón que lleva
        // directo a /Pedidos/Instrucciones (misma regla del guard
        // PuedeSubirComprobante: Nuevo + Pendiente + sin comprobante + no vencido).

        [Fact]
        public async Task MisPedidos_TransferenciaSinComprobante_MuestraAtajoDeSubida()
        {
            var ids = SembrarPedidoTransferencia(duenoEmail: "dueno-comprobante@test.local");

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Falta comprobante", html);
            Assert.Contains("Subir comprobante", html);
            Assert.Contains($"/Pedidos/Instrucciones?id={ids.PedidoId}", html);
        }

        [Fact]
        public async Task MisPedidos_TransferenciaConComprobante_NoMuestraAtajo()
        {
            var ids = SembrarPedidoTransferencia(
                duenoEmail: "dueno-con-comprobante@test.local",
                comprobanteUrl: "https://res.cloudinary.com/test/comprobante.png");

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Subir comprobante", html);
            Assert.DoesNotContain("Falta comprobante", html);
            Assert.Contains("Ver detalles", html);
        }

        [Fact]
        public async Task MisPedidos_TransferenciaVencida_NoMuestraAtajo()
        {
            // Pedido de hace 25h: el job de vencimiento (o el guard de
            // Instrucciones) ya no permite subir comprobante.
            var ids = SembrarPedidoTransferencia(
                duenoEmail: "dueno-vencido@test.local",
                fechaPedido: DateTime.Now.AddHours(-25));

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Subir comprobante", html);
        }

        [Fact]
        public async Task MisPedidos_MercadoPagoPendiente_NoMuestraAtajo()
        {
            // Solo transferencia ofrece el atajo: MP pendiente reintenta por
            // ReintentarPago (flujo de PagoFallido), no sube comprobante.
            var ids = _app.WithDb(ctx =>
            {
                // FK Pedido→MetodoDePago: si el row id 1 no existe lo siembro
                // explícito (el guard compara contra el id fijo 2 de transferencia).
                var mp = ctx.Set<MetodoDePago>().FirstOrDefault(m => m.MetodoDePagoId == 1);
                if (mp == null)
                {
                    mp = new MetodoDePago { MetodoDePagoId = 1, Nombre = "Mercado Pago" };
                    ctx.Set<MetodoDePago>().Add(mp);
                    ctx.SaveChanges();
                }

                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-mp@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, dueno, producto, 1, metodoDePagoId: mp.MetodoDePagoId);
                return (PedidoId: pedido.PedidoId, UsuarioId: dueno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Subir comprobante", html);
            Assert.DoesNotContain("Falta comprobante", html);
        }

        // Filtro por estado client-side: TODAS las tarjetas viajan al DOM con su
        // data-estado-grupo (MisPedidos.js oculta/muestra sin recargar) y el
        // server pre-marca el chip activo + pre-oculta tarjetas según ?estado=.

        [Fact]
        public async Task MisPedidos_TarjetasViajanConGrupoDeEstado()
        {
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-filtro@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var entregado = TestDatabase.SeedPedido(ctx, dueno, producto, 1, estado: EstadoPedido.Entregado);
                var nuevo = TestDatabase.SeedPedido(ctx, dueno, producto, 1);
                return (EntregadoId: entregado.PedidoId, NuevoId: nuevo.PedidoId, UsuarioId: dueno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            // Ambas tarjetas al DOM (nada se filtra en el server)...
            Assert.Contains($"#{ids.EntregadoId}", html);
            Assert.Contains($"#{ids.NuevoId}", html);
            // ...cada una con su grupo para el filtro client-side.
            Assert.Contains("data-estado-grupo=\"entregados\"", html);
            Assert.Contains("data-estado-grupo=\"enCurso\"", html);
            // Los conteos se calculan sobre la lista completa.
            Assert.Contains("Todos (2)", html);
            Assert.Contains("Entregados (1)", html);
        }

        [Fact]
        public async Task MisPedidos_ChipActivo_SegunParametroEstado()
        {
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-enCurso@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var nuevo = TestDatabase.SeedPedido(ctx, dueno, producto, 1);
                var cancelado = TestDatabase.SeedPedido(ctx, dueno, producto, 1, estado: EstadoPedido.Cancelado);
                return (NuevoId: nuevo.PedidoId, CanceladoId: cancelado.PedidoId, UsuarioId: dueno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos?estado=cancelados");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            // El chip cancelados queda activo (único con aria-pressed=true).
            Assert.Contains("aria-pressed=\"true\"", html);
            Assert.Contains("Cancelados (1)", html);
            // La tarjeta ajena al filtro viaja pre-oculta: refresh o link compartido sin flash.
            Assert.Contains("card-pedido d-none", html);
        }

        private (int PedidoId, int UsuarioId) SembrarPedidoTransferencia(
            string duenoEmail, string? comprobanteUrl = null, DateTime? fechaPedido = null)
        {
            return _app.WithDb(ctx =>
            {
                // El guard compara contra el id fijo 2 de la tabla MetodoDePago.
                var transferencia = ctx.Set<MetodoDePago>().FirstOrDefault(m => m.MetodoDePagoId == 2);
                if (transferencia == null)
                {
                    transferencia = new MetodoDePago { MetodoDePagoId = 2, Nombre = "Transferencia" };
                    ctx.Set<MetodoDePago>().Add(transferencia);
                    ctx.SaveChanges();
                }

                var dueno = TestDatabase.SeedUsuario(ctx, duenoEmail);
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, dueno, producto, 1,
                    metodoDePagoId: transferencia.MetodoDePagoId,
                    comprobanteUrl: comprobanteUrl,
                    fechaPedido: fechaPedido);
                return (PedidoId: pedido.PedidoId, UsuarioId: dueno.UsuarioId);
            });
        }
    }
}
