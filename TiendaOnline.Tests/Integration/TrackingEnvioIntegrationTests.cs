using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El tracking del pedido en el front del cliente: el timeline de Detalles
    /// se arma por historia (cada hito con su fecha — un entregado muestra
    /// envío y entrega, cada uno con la suya) y la card de Mis Pedidos muestra
    /// cuánto hace que salió. Contra la app real con el dueño autenticado.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class TrackingEnvioIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public TrackingEnvioIntegrationTests(TestApp app)
        {
            _app = app;
        }

        [Fact]
        public async Task Detalles_Entregado_MuestraLaFechaDeEnvioYLaDeEntrega()
        {
            // El paso "en camino" de un pedido entregado lleva la fecha de ENVÍO;
            // la de ENTREGA vive en su propio hito, no reemplaza la anterior.
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-entregado@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(
                    ctx, dueno, producto, 1,
                    estado: EstadoPedido.Entregado,
                    estadoPago: EstadoPago.Aprobado,
                    fechaEnPreparacion: DateTime.Now.AddDays(-3),
                    fechaEnvio: DateTime.Now.AddDays(-2),
                    fechaEntrega: DateTime.Now.AddDays(-1));
                return (PedidoId: pedido.PedidoId, UsuarioId: dueno.UsuarioId,
                    Preparacion: pedido.FechaEnPreparacion!.Value.ToString("dd/MM/yyyy HH:mm"),
                    Envio: pedido.FechaEnvio!.Value.ToString("dd/MM/yyyy HH:mm"),
                    Entrega: pedido.FechaEntrega!.Value.ToString("dd/MM/yyyy HH:mm"));
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync($"/Pedidos/Detalles/{ids.PedidoId}");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            // Los tres hitos con su fecha propia (no repetida)
            Assert.Contains("Pago confirmado", html);
            Assert.Contains(ids.Preparacion, html);
            Assert.Contains(ids.Envio, html);
            Assert.Contains(ids.Entrega, html);
            Assert.Contains("¡Pedido entregado!", html);
        }

        [Fact]
        public async Task Detalles_EnPreparacion_MuestraElHitoDePagoConfirmado()
        {
            // El hito de preparación viaja con su fecha: sin FechaEnPreparacion en
            // el DTO, el timeline de un pedido en preparación salta ese paso.
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-preparacion@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(
                    ctx, dueno, producto, 1,
                    estado: EstadoPedido.EnPreparacion,
                    estadoPago: EstadoPago.Aprobado);
                return (PedidoId: pedido.PedidoId, UsuarioId: dueno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync($"/Pedidos/Detalles/{ids.PedidoId}");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Pago confirmado", html);
            // El estado no avanzó: el hito de envío no aparece todavía
            Assert.DoesNotContain("Pedido en camino", html);
        }

        [Fact]
        public async Task Detalles_Nuevo_NoMuestraLosPasosQueNoLlegaron()
        {
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-nuevo@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(ctx, dueno, producto, 1);
                return (PedidoId: pedido.PedidoId, UsuarioId: dueno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync($"/Pedidos/Detalles/{ids.PedidoId}");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Pago confirmado", html);
            Assert.DoesNotContain("Pedido en camino", html);
            Assert.DoesNotContain("¡Pedido entregado!", html);
        }

        [Fact]
        public async Task MisPedidos_Enviado_MuestraCuantoHaceQueSalio()
        {
            var ids = _app.WithDb(ctx =>
            {
                var dueno = TestDatabase.SeedUsuario(ctx, "dueno-camino@test.local");
                var producto = TestDatabase.SeedProducto(ctx);
                var pedido = TestDatabase.SeedPedido(
                    ctx, dueno, producto, 1,
                    estado: EstadoPedido.Enviado,
                    estadoPago: EstadoPago.Aprobado,
                    fechaEnvio: DateTime.Now.AddHours(-3));
                return (PedidoId: pedido.PedidoId, UsuarioId: dueno.UsuarioId);
            });

            var client = _app.CreateClientAs(ids.UsuarioId);

            var response = await client.GetAsync("/Pedidos/MisPedidos");
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("hace 3 h", html);
        }
    }
}
