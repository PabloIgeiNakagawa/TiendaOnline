using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Services
{
    public class PedidoServiceTests : IDisposable
    {
        private readonly TiendaContext _db;
        private readonly Mock<IAuditoriaService> _mockAuditoria;
        private readonly PedidoService _service;

        public PedidoServiceTests()
        {
            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TiendaContext(options);
            _mockAuditoria = new Mock<IAuditoriaService>();
            _service = new PedidoService(_db, _mockAuditoria.Object);
        }

        #region Tests de Creación

        [Fact]
        public async Task CrearPedidoAsync_DebeGuardarCorrectamente_Y_RegistrarAuditoria()
        {
            // ARRANGE
            var carrito = new List<ItemCarrito>
            {
                new ItemCarrito { ProductoId = 1, Cantidad = 2, Precio = 100 },
                new ItemCarrito { ProductoId = 2, Cantidad = 1, Precio = 500 }
            };
            int usuarioId = 10;

            // ACT
            var pedidoId = await _service.CrearPedidoAsync(carrito, usuarioId);

            // ASSERT
            var pedido = await _db.Pedidos.Include(p => p.DetallesPedido).FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

            Assert.NotNull(pedido);
            Assert.Equal(usuarioId, pedido.UsuarioId);
            Assert.Equal(2, pedido.DetallesPedido.Count);
            // Verificamos el cálculo: (2 * 100) + (1 * 500) = 700
            Assert.Equal(700, pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario));

            // Verificar que se disparó la auditoría
            _mockAuditoria.Verify(a => a.RegistrarAccionAsync(
                "Crear Pedido",
                null,
                It.IsAny<object>()
            ), Times.Once);
        }

        #endregion

        #region Tests de Cambio de Estado

        [Fact]
        public async Task PedidoEnviadoAsync_DebeActualizarEstadoYFecha()
        {
            // ARRANGE
            var pedido = new Pedido { PedidoId = 1, Estado = EstadoPedido.Pendiente };
            _db.Pedidos.Add(pedido);
            await _db.SaveChangesAsync();

            // ACT
            await _service.PedidoEnviadoAsync(1);

            // ASSERT
            var resultado = await _db.Pedidos.FindAsync(1);
            Assert.Equal(EstadoPedido.Enviado, resultado.Estado);
            Assert.NotNull(resultado.FechaEnvio);

            // Verificamos que la auditoría recibió el estado anterior (Pendiente)
            _mockAuditoria.Verify(a => a.RegistrarAccionAsync(
                "Enviar Pedido",
                It.Is<object>(old => old.ToString().Contains("Pendiente")),
                It.IsAny<object>()
            ), Times.Once);
        }

        [Fact]
        public async Task PedidoCanceladoAsync_DebeLanzarExcepcion_SiIdNoExiste()
        {
            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.PedidoCanceladoAsync(999));
            Assert.Equal("Pedido no encontrado.", ex.Message);
        }

        #endregion

        #region Tests de Consultas

        [Fact]
        public async Task ObtenerPedidosDeUsuarioAsync_DebeFiltrarCorrectamente()
        {
            // ARRANGE
            _db.Pedidos.AddRange(
                new Pedido { PedidoId = 1, UsuarioId = 1 },
                new Pedido { PedidoId = 2, UsuarioId = 1 },
                new Pedido { PedidoId = 3, UsuarioId = 2 }
            );
            await _db.SaveChangesAsync();

            // ACT
            var pedidosUser1 = await _service.ObtenerPedidosDeUsuarioAsync(1);

            // ASSERT
            Assert.Equal(2, pedidosUser1.Count);
            Assert.All(pedidosUser1, p => Assert.Equal(1, p.UsuarioId));
        }

        #endregion

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}