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
    public class CategoriaControllerTests
    {
        private readonly Mock<ICategoriaService> _mockService;
        private readonly CategoriaController _controller;

        public CategoriaControllerTests()
        {
            _mockService = new Mock<ICategoriaService>();
            _controller = new CategoriaController(_mockService.Object);

            // Setup para TempData (necesario porque el controlador lo usa)
            var tempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [Fact]
        public async Task AgregarCategoria_DebeRetornarVistaConSelectList()
        {
            // ARRANGE
            _mockService.Setup(s => s.ObtenerCategoriasAsync()).ReturnsAsync(new List<Categoria>());

            // ACT
            var result = await _controller.AgregarCategoria();

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<SelectList>(viewResult.ViewData["Categorias"]);
        }

        [Fact]
        public async Task Crear_Categoria_DebeRedirigir_CuandoEsExitoso()
        {
            // ARRANGE
            var nuevaCat = new Categoria { Nombre = "Hardware" };
            _mockService.Setup(s => s.ExisteNombreAsync(nuevaCat.Nombre)).ReturnsAsync(false);

            // ACT
            var result = await _controller.Crear(nuevaCat);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("Categorias", redirect.ActionName);
            Assert.Equal("La categoría se creó correctamente.", _controller.TempData["MensajeExito"]);
        }

        [Fact]
        public async Task Crear_Categoria_DebeRetornarVista_SiNombreYaExiste()
        {
            // ARRANGE
            var nuevaCat = new Categoria { Nombre = "Existente" };
            _mockService.Setup(s => s.ExisteNombreAsync(nuevaCat.Nombre)).ReturnsAsync(true);
            _mockService.Setup(s => s.ObtenerCategoriasAsync()).ReturnsAsync(new List<Categoria>());

            // ACT
            var result = await _controller.Crear(nuevaCat);

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("Nombre"));
        }

        [Fact]
        public async Task Editar_Categoria_DebeManejarExcepcionDeBucle()
        {
            // ARRANGE
            var cat = new Categoria { CategoriaId = 1, Nombre = "Error" };
            _mockService.Setup(s => s.CambiarCategoriaPadre(It.IsAny<int>(), It.IsAny<int?>()))
                        .ThrowsAsync(new InvalidOperationException("No se puede asignar como hija de sí misma"));
            _mockService.Setup(s => s.ObtenerCategoriasAsync()).ReturnsAsync(new List<Categoria>());

            // ACT
            var result = await _controller.Editar(cat);

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Eliminar_DebeSetearErrorEnTempData_SiFalla()
        {
            // ARRANGE
            _mockService.Setup(s => s.EliminarCategoriaAsync(1)).ReturnsAsync(false);

            // ACT
            await _controller.Eliminar(1);

            // ASSERT
            Assert.NotNull(_controller.TempData["Error"]);
        }
    }
}