using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Newtonsoft.Json;
using System.Text;
using TiendaOnline.Controllers;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using Xunit;

namespace TiendaOnline.Tests.Controllers
{
    public class CarritoControllerTests
    {
        private readonly Mock<IProductoService> _mockProductoService;
        private readonly CarritoController _controller;
        private readonly Mock<ISession> _mockSession;

        public CarritoControllerTests()
        {
            _mockProductoService = new Mock<IProductoService>();
            _mockSession = new Mock<ISession>();

            // Configuramos el contexto del controlador para que use nuestra sesión mockeada
            var httpContext = new DefaultHttpContext();
            httpContext.Session = _mockSession.Object;

            _controller = new CarritoController(_mockProductoService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Agregar_DebeCrearNuevoItem_SiCarritoEstaVacio()
        {
            // ARRANGE
            int productoId = 1;
            var producto = new Producto { ProductoId = productoId, Nombre = "Mouse", Precio = 50, ImagenUrl = "url" };

            _mockProductoService.Setup(s => s.ObtenerProductoAsync(productoId)).ReturnsAsync(producto);

            // Simulamos que la sesión devuelve null (carrito vacío)
            byte[] outValue = null;
            _mockSession.Setup(s => s.TryGetValue("Carrito", out outValue)).Returns(false);

            // ACT
            var result = await _controller.Agregar(productoId);

            // ASSERT
            _mockSession.Verify(s => s.Set("Carrito", It.IsAny<byte[]>()), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Agregar_DebeIncrementarCantidad_SiProductoYaExiste()
        {
            // ARRANGE
            int productoId = 1;
            var carritoExistente = new List<ItemCarrito>
            {
                new ItemCarrito { ProductoId = productoId, Cantidad = 1 }
            };

            // Simulamos que la sesión YA tiene el carrito (serializado a JSON)
            var json = JsonConvert.SerializeObject(carritoExistente);
            var bytes = Encoding.UTF8.GetBytes(json);
            _mockSession.Setup(s => s.TryGetValue("Carrito", out bytes)).Returns(true);

            _mockProductoService.Setup(s => s.ObtenerProductoAsync(productoId))
                                .ReturnsAsync(new Producto { ProductoId = productoId });

            // ACT
            await _controller.Agregar(productoId);

            // ASSERT
            // Verificamos que se guardó el carrito con cantidad = 2
            _mockSession.Verify(s => s.Set("Carrito", It.Is<byte[]>(b =>
                Encoding.UTF8.GetString(b).Contains("\"Cantidad\":2")
            )), Times.Once);
        }

        [Fact]
        public void Vaciar_DebeRemoverKeyDeSesion()
        {
            // ACT
            var result = _controller.Vaciar();

            // ASSERT
            _mockSession.Verify(s => s.Remove("Carrito"), Times.Once);
            Assert.Equal("Se ha vacíado el carrito.", _controller.TempData["MensajeExito"]);
        }

        [Fact]
        public void ActualizarCantidad_DebeModificarItemCorrecto()
        {
            // ARRANGE
            var carrito = new List<ItemCarrito> { new ItemCarrito { ProductoId = 10, Cantidad = 1 } };
            var json = JsonConvert.SerializeObject(carrito);
            var bytes = Encoding.UTF8.GetBytes(json);
            _mockSession.Setup(s => s.TryGetValue("Carrito", out bytes)).Returns(true);

            // ACT
            _controller.ActualizarCantidad(10, 5);

            // ASSERT
            _mockSession.Verify(s => s.Set("Carrito", It.Is<byte[]>(b =>
                Encoding.UTF8.GetString(b).Contains("\"Cantidad\":5")
            )), Times.Once);
        }
    }
}