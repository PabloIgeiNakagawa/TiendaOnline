using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using TiendaOnline.Controllers;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Controllers
{
    public class PedidoControllerTests
    {
        private readonly Mock<IPedidoService> _mockPedidoService;
        private readonly Mock<IUsuarioService> _mockUsuarioService;
        private readonly PedidoController _controller;

        public PedidoControllerTests()
        {
            _mockPedidoService = new Mock<IPedidoService>();
            _mockUsuarioService = new Mock<IUsuarioService>();
            _controller = new PedidoController(_mockPedidoService.Object, _mockUsuarioService.Object);

            // Simular un usuario logueado con ID 99
            var claims = new List<Claim> { new Claim("UsuarioId", "99") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = user };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task MisPedidos_DebeRetornarVistaConPedidosDelUsuario()
        {
            // ARRANGE
            _mockPedidoService.Setup(s => s.ObtenerPedidosDeUsuarioAsync(99))
                              .ReturnsAsync(new List<Pedido> { new Pedido { PedidoId = 1 } });

            // ACT
            var result = await _controller.MisPedidos();

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Pedido>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task FinalizarCompra_DebeRedirigirADetalles_SiEsExitoso()
        {
            // ARRANGE
            var carrito = new List<ItemCarrito> { new ItemCarrito { ProductoId = 1, Cantidad = 1 } };
            _mockPedidoService.Setup(s => s.CrearPedidoAsync(carrito, 99)).ReturnsAsync(500);

            // ACT
            var result = await _controller.FinalizarCompra(carrito);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Detalles", redirect.ActionName);
            Assert.Equal(500, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task FinalizarCompra_DebeRedirigirACarrito_SiCarritoEstaVacio()
        {
            // ACT
            var result = await _controller.FinalizarCompra(null);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Carrito", redirect.ControllerName);
            Assert.Equal("El carrito está vacío.", _controller.TempData["MensajeError"]);
        }

        [Fact]
        public async Task CambiarEstado_DebeLlamarAlMetodoCorrecto_SegunEstado()
        {
            // ARRANGE
            int pedidoId = 10;

            // ACT
            await _controller.CambiarEstadoAsync(pedidoId, EstadoPedido.Enviado);

            // ASSERT
            _mockPedidoService.Verify(s => s.PedidoEnviadoAsync(pedidoId), Times.Once);
        }
    }
}