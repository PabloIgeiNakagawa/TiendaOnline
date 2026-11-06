using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Pedidos
{
    /// <summary>
    /// Tests del flujo de pago por transferencia bancaria: subida de comprobante,
    /// aprobación manual (misma maquinaria de reserva de stock que Mercado Pago)
    /// y rechazo del comprobante (cancelación + baja del archivo en Cloudinary).
    /// Los guards de dominio lanzan excepciones tipadas (TECH_DEBT #7): se aserte el
    /// tipo concreto, no Exception genérica (xUnit exige match exacto).
    /// </summary>
    public class PedidoCommandServiceTransferenciaTests
    {
        private const string UrlComprobante = "https://res.cloudinary.com/demo/Comprobantes/Pedido-000001.jpg";
        private const string PublicIdComprobante = "Comprobantes/Pedido-000001.jpg";

        /// <summary>
        /// Seed del escenario base. El orden de los métodos de pago replica la migration
        /// real: "Mercado Pago" (id 1) y "Transferencia Bancaria" (id 2), porque el
        /// servicio usa la constante 2 para identificar transferencia.
        /// </summary>
        private static (Usuario Usuario, Producto Producto, Pedido Pedido, int MercadoPagoId, int TransferenciaId) SeedEscenario(
            TiendaContext ctx,
            int stock = 5,
            int cantidad = 2,
            string? comprobanteUrl = UrlComprobante,
            DateTime? fechaPedido = null,
            EstadoPedido estado = EstadoPedido.Nuevo,
            EstadoPago estadoPago = EstadoPago.Pendiente,
            int? metodoDePagoId = null,
            decimal costoEnvio = 0)
        {
            var mp = TestDatabase.SeedMetodoDePago(ctx, "Mercado Pago");              // id 1, como en la migration
            var transferencia = TestDatabase.SeedMetodoDePago(ctx, "Transferencia Bancaria"); // id 2
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: stock);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad,
                estado: estado,
                estadoPago: estadoPago,
                fechaPedido: fechaPedido,
                costoEnvio: costoEnvio,
                metodoDePagoId: metodoDePagoId ?? transferencia.MetodoDePagoId,
                comprobanteUrl: comprobanteUrl);

            return (usuario, producto, pedido, mp.MetodoDePagoId, transferencia.MetodoDePagoId);
        }

        // ------------------------------------------------------------------
        // GuardarComprobanteAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task GuardarComprobante_SubidaExitosa_GuardaUrlYFecha()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx, comprobanteUrl: null);

            var imagen = new Mock<IImagenService>();
            imagen.Setup(x => x.SubirComprobanteAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                  .ReturnsAsync(UrlComprobante);
            var settings = new Mock<IAppSettingsService>();
            settings.Setup(x => x.GetValue("Pagos:EmailNotificaciones")).Returns("dueno@techstore.com");
            var sut = Sut.PedidoCommand(ctx, imagen: imagen, settings: settings);

            var url = await sut.GuardarComprobanteAsync(pedido.PedidoId, Stream.Null, "Pedido-000001.jpg");

            Assert.Equal(UrlComprobante, url);
            imagen.Verify(x => x.SubirComprobanteAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(UrlComprobante, pedidoDb.ComprobanteUrl);
            Assert.NotNull(pedidoDb.FechaComprobante);
        }

        [Fact]
        public async Task GuardarComprobante_PedidoInexistente_LanzaPedidoNoEncontradoException()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoEncontradoException>(
                () => sut.GuardarComprobanteAsync(99999, Stream.Null, "Pedido-000001.jpg"));
        }

        [Fact]
        public async Task GuardarComprobante_CloudinaryDevuelveVacio_LanzaYNoModificaPedido()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx, comprobanteUrl: null);

            var imagen = new Mock<IImagenService>();
            imagen.Setup(x => x.SubirComprobanteAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                  .ReturnsAsync((string?)null);
            var sut = Sut.PedidoCommand(ctx, imagen: imagen);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.GuardarComprobanteAsync(pedido.PedidoId, Stream.Null, "Pedido-000001.jpg"));

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Null(pedidoDb.ComprobanteUrl); // fallo Cloudinary no deja rastro en la base
            Assert.Null(pedidoDb.FechaComprobante);
        }

        [Fact]
        public async Task GuardarComprobante_MetodoDePagoDistinto_LanzaYNoSubeArchivo()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            // Pedido de Mercado Pago (id 1): la subida de comprobante no le aplica
            var mp = TestDatabase.SeedMetodoDePago(ctx, "Mercado Pago");
            TestDatabase.SeedMetodoDePago(ctx, "Transferencia Bancaria");
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                metodoDePagoId: mp.MetodoDePagoId);

            var imagen = new Mock<IImagenService>();
            var sut = Sut.PedidoCommand(ctx, imagen: imagen);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.GuardarComprobanteAsync(pedido.PedidoId, Stream.Null, "Pedido-000001.jpg"));

            imagen.Verify(x => x.SubirComprobanteAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GuardarComprobante_YaTieneComprobante_LanzaYNoResube()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx, comprobanteUrl: UrlComprobante);

            var imagen = new Mock<IImagenService>();
            var sut = Sut.PedidoCommand(ctx, imagen: imagen);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.GuardarComprobanteAsync(pedido.PedidoId, Stream.Null, "Pedido-000001.jpg"));

            imagen.Verify(x => x.SubirComprobanteAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GuardarComprobante_PedidoYaConfirmado_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado,
                comprobanteUrl: null);

            var imagen = new Mock<IImagenService>();
            var sut = Sut.PedidoCommand(ctx, imagen: imagen);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.GuardarComprobanteAsync(pedido.PedidoId, Stream.Null, "Pedido-000001.jpg"));

            imagen.Verify(x => x.SubirComprobanteAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
        }

        // ------------------------------------------------------------------
        // AprobarPagoTransferenciaAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task AprobarPagoTransferencia_ComprobanteValido_ReservaStockGeneraSalidaYPrepara()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, producto, pedido, _, _) = SeedEscenario(ctx, stock: 5, cantidad: 2, costoEnvio: 50m);
            var sut = Sut.PedidoCommand(ctx);

            await sut.AprobarPagoTransferenciaAsync(pedido.PedidoId);

            using var verify = db.CreateContext();

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Aprobado, pedidoDb.EstadoPago);
            Assert.Equal(EstadoPedido.EnPreparacion, pedidoDb.Estado);
            Assert.NotNull(pedidoDb.FechaEnPreparacion);

            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(3, stock); // 5 - 2

            var movimiento = await verify.MovimientosStock
                .Where(m => m.PedidoId == pedido.PedidoId)
                .SingleAsync();
            Assert.Equal(TipoMovimiento.SalidaVenta, movimiento.Tipo);
            Assert.Equal(-2, movimiento.Cantidad);
            Assert.Equal(producto.ProductoId, movimiento.ProductoId);
        }

        [Fact]
        public async Task AprobarPagoTransferencia_SinComprobante_LanzaYNoReserva()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, producto, pedido, _, _) = SeedEscenario(ctx, comprobanteUrl: null);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.AprobarPagoTransferenciaAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock);

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Nuevo, pedidoDb.Estado);

            Assert.False(await verify.MovimientosStock.AnyAsync(m => m.PedidoId == pedido.PedidoId));
        }

        [Fact]
        public async Task AprobarPagoTransferencia_YaAprobado_LanzaYNoDuplica()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, producto, pedido, _, _) = SeedEscenario(ctx,
                estado: EstadoPedido.EnPreparacion, estadoPago: EstadoPago.Aprobado);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(
                () => sut.AprobarPagoTransferenciaAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock); // no descuenta stock dos veces

            Assert.False(await verify.MovimientosStock.AnyAsync(m => m.PedidoId == pedido.PedidoId));
        }

        [Fact]
        public async Task AprobarPagoTransferencia_PedidoVencido_LanzaYNoReserva()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, producto, pedido, _, _) = SeedEscenario(ctx, fechaPedido: DateTime.Now.AddHours(-25));
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(
                () => sut.AprobarPagoTransferenciaAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock);

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Nuevo, pedidoDb.Estado); // el job de vencimiento le toca cancelarlo, no la aprobación
        }

        [Fact]
        public async Task AprobarPagoTransferencia_StockInsuficiente_LanzaYNoCambiaNada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, producto, pedido, _, _) = SeedEscenario(ctx, stock: 1, cantidad: 2);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<StockInsuficienteException>(
                () => sut.AprobarPagoTransferenciaAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(1, stock); // reserva fallida no rosca el stock

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Nuevo, pedidoDb.Estado);
            Assert.Equal(EstadoPago.Pendiente, pedidoDb.EstadoPago);

            Assert.False(await verify.MovimientosStock.AnyAsync(m => m.PedidoId == pedido.PedidoId));
        }

        [Fact]
        public async Task AprobarPagoTransferencia_MetodoDePagoDistinto_LanzaYNoReserva()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            // Pedido de Mercado Pago (id 1): la aprobación manual de transferencia no le aplica
            var mp = TestDatabase.SeedMetodoDePago(ctx, "Mercado Pago");
            TestDatabase.SeedMetodoDePago(ctx, "Transferencia Bancaria");
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                metodoDePagoId: mp.MetodoDePagoId);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.AprobarPagoTransferenciaAsync(pedido.PedidoId));

            using var verify = db.CreateContext();
            var stock = await verify.Productos.AsNoTracking()
                .Where(p => p.ProductoId == producto.ProductoId).Select(p => p.Stock).SingleAsync();
            Assert.Equal(5, stock);

            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPago.Pendiente, pedidoDb.EstadoPago);
        }

        // ------------------------------------------------------------------
        // RechazarComprobanteTransferenciaAsync
        // ------------------------------------------------------------------

        [Fact]
        public async Task RechazarComprobante_Valido_CancelaPedidoYBajaComprobante()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx);

            var imagen = new Mock<IImagenService>();
            imagen.Setup(x => x.ExtraerPublicIdDesdeUrl(UrlComprobante)).Returns(PublicIdComprobante);
            var sut = Sut.PedidoCommand(ctx, imagen: imagen);

            await sut.RechazarComprobanteTransferenciaAsync(pedido.PedidoId);

            // El archivo se da de baja con el publicId derivado de la URL
            imagen.Verify(x => x.BorrarImagenAsync(PublicIdComprobante), Times.Once);

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Cancelado, pedidoDb.Estado);
            Assert.Equal(EstadoPago.Rechazado, pedidoDb.EstadoPago);
            Assert.NotNull(pedidoDb.FechaCancelado);
        }

        [Fact]
        public async Task RechazarComprobante_SinComprobante_LanzaYNoCambiaEstado()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx, comprobanteUrl: null);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.RechazarComprobanteTransferenciaAsync(pedido.PedidoId));

            // Sin comprobante existe el botón Cancelar; rechazar no debe tocar nada

            using var verify = db.CreateContext();
            var pedidoDb = await verify.Pedidos.AsNoTracking().FirstAsync(p => p.PedidoId == pedido.PedidoId);
            Assert.Equal(EstadoPedido.Nuevo, pedidoDb.Estado);
            Assert.Equal(EstadoPago.Pendiente, pedidoDb.EstadoPago);
        }

        [Fact]
        public async Task RechazarComprobante_PedidoYaCancelado_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (_, _, pedido, _, _) = SeedEscenario(ctx, estado: EstadoPedido.Cancelado, estadoPago: EstadoPago.Rechazado);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<PedidoNoCancelableException>(
                () => sut.RechazarComprobanteTransferenciaAsync(pedido.PedidoId));
        }

        [Fact]
        public async Task RechazarComprobante_MetodoDePagoDistinto_LanzaExcepcion()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            // Pedido de Mercado Pago (id 1): el rechazo de comprobante no le aplica
            var mp = TestDatabase.SeedMetodoDePago(ctx, "Mercado Pago");
            TestDatabase.SeedMetodoDePago(ctx, "Transferencia Bancaria");
            var usuario = TestDatabase.SeedUsuario(ctx);
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var pedido = TestDatabase.SeedPedido(ctx, usuario, producto, cantidad: 2,
                metodoDePagoId: mp.MetodoDePagoId);
            var sut = Sut.PedidoCommand(ctx);

            await Assert.ThrowsAsync<ComprobanteNoValidoException>(
                () => sut.RechazarComprobanteTransferenciaAsync(pedido.PedidoId));
        }
    }
}
