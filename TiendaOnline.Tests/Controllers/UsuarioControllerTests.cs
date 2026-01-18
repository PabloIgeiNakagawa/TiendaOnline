using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using TiendaOnline.Controllers;
using TiendaOnline.Exceptions;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Controllers
{
    public class UsuarioControllerTests
    {
        private readonly Mock<IUsuarioService> _mockUsuarioService;
        private readonly UsuarioController _controller;

        public UsuarioControllerTests()
        {
            _mockUsuarioService = new Mock<IUsuarioService>();
            _controller = new UsuarioController(_mockUsuarioService.Object);

            // Setup necesario para TempData y el contexto de autenticación
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Register_Post_DebeRedirigir_CuandoEsExitoso()
        {
            // ARRANGE
            var nuevoUsuario = new Usuario { Nombre = "Juan", Email = "juan@test.com" };

            // ACT
            var result = await _controller.Register(nuevoUsuario);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Usuarios", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            _mockUsuarioService.Verify(s => s.CrearUsuarioAsync(nuevoUsuario), Times.Once);
        }

        [Fact]
        public async Task Register_Post_DebeRetornarVista_CuandoEmailEstaDuplicado()
        {
            // ARRANGE
            var usuario = new Usuario { Email = "duplicado@test.com" };
            _mockUsuarioService.Setup(s => s.CrearUsuarioAsync(It.IsAny<Usuario>()))
                               .ThrowsAsync(new EmailDuplicadoException("El email ya existe"));

            // ACT
            var result = await _controller.Register(usuario);

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("Email"));
        }

        [Fact]
        public async Task Login_Post_DebeRetornarError_SiUsuarioNoExiste()
        {
            // ARRANGE
            var loginModel = new Login { Email = "noexiste@test.com", Contrasena = "123" };
            _mockUsuarioService.Setup(s => s.ObtenerPorEmailAsync(loginModel.Email))
                               .ReturnsAsync((Usuario)null);

            // ACT
            var result = await _controller.Login(loginModel);

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task DarBajaUsuario_DebeRedirigirAAdminUsuarios()
        {
            // ACT
            var result = await _controller.DarBajaUsuario(10);

            // ASSERT
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Usuarios", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("El usuario se dió de baja correctamente.", _controller.TempData["MensajeExito"]);
            _mockUsuarioService.Verify(s => s.DarBajaUsuarioAsync(10), Times.Once);
        }

        [Fact]
        public async Task PerfilUsuario_DebeRetornarVistaConUsuario()
        {
            // ARRANGE
            var usuarioFake = new Usuario { UsuarioId = 1, Nombre = "Test User" };
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioAsync(1)).ReturnsAsync(usuarioFake);

            // ACT
            var result = await _controller.PerfilUsuario(1);

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Usuario>(viewResult.Model);
            Assert.Equal(1, model.UsuarioId);
        }
    }
}