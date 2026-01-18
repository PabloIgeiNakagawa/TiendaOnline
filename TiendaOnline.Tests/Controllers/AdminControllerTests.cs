using Microsoft.AspNetCore.Mvc;
using Moq;
using TiendaOnline.Controllers; // Ajustá según tu namespace
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IProductoService> _mockProducto;
        private readonly Mock<IUsuarioService> _mockUsuario;
        private readonly Mock<ICategoriaService> _mockCategoria;
        private readonly Mock<IPedidoService> _mockPedido;
        private readonly Mock<IReportesService> _mockReportes;
        private readonly Mock<IAuditoriaService> _mockAuditoria;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            // Inicializamos todos los mocks
            _mockProducto = new Mock<IProductoService>();
            _mockUsuario = new Mock<IUsuarioService>();
            _mockCategoria = new Mock<ICategoriaService>();
            _mockPedido = new Mock<IPedidoService>();
            _mockReportes = new Mock<IReportesService>();
            _mockAuditoria = new Mock<IAuditoriaService>();

            // Inyectamos los mocks en el controlador
            _controller = new AdminController(
                _mockProducto.Object,
                _mockUsuario.Object,
                _mockCategoria.Object,
                _mockPedido.Object,
                _mockReportes.Object,
                _mockAuditoria.Object
            );
        }

        [Fact]
        public async Task Usuarios_DebeRetornarVistaConListaDeUsuarios()
        {
            // ARRANGE
            var usuariosFake = new List<Usuario> { new Usuario { Nombre = "Admin" } };
            _mockUsuario.Setup(s => s.ObtenerUsuariosAsync()).ReturnsAsync(usuariosFake);

            // ACT
            var result = await _controller.Usuarios();

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Productos_DebeCargarCategoriasEnViewBag()
        {
            // ARRANGE
            _mockProducto.Setup(s => s.ObtenerProductosAsync()).ReturnsAsync(new List<Producto>());
            _mockCategoria.Setup(s => s.ObtenerCategoriasAsync()).ReturnsAsync(new List<Categoria>());

            // ACT
            var result = await _controller.Productos();

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["Categorias"]);
            _mockCategoria.Verify(s => s.ObtenerCategoriasAsync(), Times.Once);
        }

        [Fact]
        public async Task DatosDashboardJson_DebeRetornarJsonResult()
        {
            // ARRANGE
            var dashboardFake = new DashboardViewModel { CantidadCancelados = 5 };
            _mockReportes.Setup(s => s.ObtenerDashboardAsync(1)).ReturnsAsync(dashboardFake);

            // ACT
            var result = await _controller.DatosDashboardJson(1);

            // ASSERT
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(dashboardFake, jsonResult.Value);
        }
    }
}