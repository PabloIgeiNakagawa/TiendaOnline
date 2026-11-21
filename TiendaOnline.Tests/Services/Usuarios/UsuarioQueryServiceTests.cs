using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Usuarios;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Usuarios
{
    /// <summary>
    /// El TotalGastado del perfil usa la regla del dinero de la casa: solo pago
    /// Aprobado, ítems + envío, misma definición que el top clientes del
    /// dashboard — un pedido Pendiente no es plata gastada.
    /// </summary>
    public class UsuarioQueryServiceTests
    {
        [Fact]
        public async Task ObtenerPerfil_PagosMixtos_SoloElAprobadoSumaConSuEnvio()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, "perfil-gasto@test.local");
            var producto = TestDatabase.SeedProducto(ctx);

            // Pagado: 2 × 50 = 100 ítems + 50 de envío → suma 150.
            TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, precioUnitario: 50m,
                estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Aprobado, costoEnvio: 50m);
            // Pendiente: nunca entró la plata → no suma (aunque cuenta como pedido).
            TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2, precioUnitario: 100m,
                estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Pendiente, costoEnvio: 20m);
            // Cancelado y reembolsado: la plata volvió → no suma ni cuenta como pedido.
            TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1, precioUnitario: 70m,
                estado: EstadoPedido.Cancelado, estadoPago: EstadoPago.Reembolsado);
            // Rechazado: nunca entró → no suma (cuenta como pedido).
            TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 1, precioUnitario: 99m,
                estado: EstadoPedido.Nuevo, estadoPago: EstadoPago.Rechazado);

            var sut = new UsuarioQueryService(ctx);

            var perfil = await sut.ObtenerPerfil(usuario.UsuarioId);

            Assert.NotNull(perfil);
            Assert.Equal(150m, perfil.TotalGastado);
            Assert.Equal(3, perfil.CantidadPedidos);
        }

        [Fact]
        public async Task ObtenerPerfil_UsuarioInexistente_DevuelveNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new UsuarioQueryService(ctx);

            var perfil = await sut.ObtenerPerfil(9999);

            Assert.Null(perfil);
        }

        [Fact]
        public async Task ObtenerUsuarioAsync_UsuarioExistente_DevuelveIdentidadSinEntidad()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var usuario = TestDatabase.SeedUsuario(ctx, "resumen-identidad@test.local");

            var sut = new UsuarioQueryService(ctx);

            var resumen = await sut.ObtenerUsuarioAsync(usuario.UsuarioId);

            Assert.NotNull(resumen);
            Assert.Equal(usuario.UsuarioId, resumen.UsuarioId);
            Assert.Equal("Test", resumen.Nombre);
            Assert.Equal("User", resumen.Apellido);
            // Sin teléfono el form muestra vacío (el claim de login hace lo mismo).
            Assert.Equal(string.Empty, resumen.Telefono);
        }

        [Fact]
        public async Task ObtenerUsuarioAsync_UsuarioInexistente_DevuelveNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new UsuarioQueryService(ctx);

            var resumen = await sut.ObtenerUsuarioAsync(9999);

            Assert.Null(resumen);
        }
    }
}
