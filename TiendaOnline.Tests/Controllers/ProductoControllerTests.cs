using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using TiendaOnline.Controllers;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using Xunit;

namespace TiendaOnline.Tests.Controllers
{
    public class ProductoControllerTests
    {
        private readonly Mock<IProductoService> _mockProductoService;
        private readonly Mock<ICategoriaService> _mockCategoriaService;
        private readonly Mock<IImagenService> _mockImagenService;
        private readonly ProductoController _controller;

        public ProductoControllerTests()
        {
            _mockProductoService = new Mock<IProductoService>();
            _mockCategoriaService = new Mock<ICategoriaService>();
            _mockImagenService = new Mock<IImagenService>();

            _controller = new ProductoController(
                _mockProductoService.Object,
                _mockCategoriaService.Object,
                _mockImagenService.Object);

            // Inicializar TempData para evitar el NullReferenceException
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        }

        [Fact]
        public async Task Index_DebeRetornarVistaConProductosYCategorias()
        {
            // ARRANGE
            _mockCategoriaService.Setup(s => s.ObtenerCategoriasRaizAsync()).ReturnsAsync(new List<Categoria>());
            _mockProductoService.Setup(s => s.ObtenerProductosAsync()).ReturnsAsync(new List<Producto>());

            // ACT
            var result = await _controller.Index("pc");

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["CategoriasRaiz"]);
            Assert.Equal("pc", viewResult.ViewData["Busqueda"]);
        }

        [Fact]
        public async Task AgregarProducto_Post_DebeRetornarError_SiNoHayImagen()
        {
            // ARRANGE
            var producto = new Producto { Nombre = "Test" };
            // Simulamos que la imagen es nula

            // ACT
            var result = await _controller.AgregarProducto(producto, null);

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("ImagenArchivo"));
        }

        [Fact]
        public async Task AgregarProducto_Post_DebeRedirigir_SiTodoEsCorrecto()
        {
            // ARRANGE
            var producto = new Producto { ProductoId = 1, Nombre = "Nuevo", CategoriaId = 5 };

            // Mock de IFormFile
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.Length).Returns(100);

            _mockCategoriaService.Setup(s => s.EsCategoriaHojaAsync(5)).ReturnsAsync(true);
            _mockImagenService.Setup(s => s.SubirImagenAsync(It.IsAny<IFormFile>())).ReturnsAsync("http://imagen.com");

            // ACT
            var result = await _controller.AgregarProducto(producto, fileMock.Object);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Detalles", redirect.ActionName);
            _mockProductoService.Verify(s => s.AgregarProductoAsync(It.IsAny<Producto>()), Times.Once);
        }

        [Fact]
        public async Task DarBajaProducto_DebeRedirigirAAdminProductos()
        {
            // ARRANGE
            int productoId = 1;

            // ACT
            var result = await _controller.DarBajaProducto(productoId);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Productos", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);

            // Verificamos que se guardó el mensaje en TempData
            Assert.Equal("El producto se dió de baja correctamente.", _controller.TempData["MensajeExito"]);

            _mockProductoService.Verify(s => s.DarBajaProductoAsync(productoId), Times.Once);
        }
    }
}