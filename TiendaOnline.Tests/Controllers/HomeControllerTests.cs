using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using TiendaOnline.Controllers;
using TiendaOnline.Models;
using Xunit;

namespace TiendaOnline.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(_mockLogger.Object);
        }

        [Fact]
        public void Index_DebeRetornarVista()
        {
            // ACT
            var result = _controller.Index();

            // ASSERT
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Privacy_DebeRetornarVista()
        {
            // ACT
            var result = _controller.Privacy();

            // ASSERT
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_DebeRetornarVistaConModeloDeError()
        {
            // ARRANGE
            // El método Error accede a HttpContext.TraceIdentifier, por lo que mockeamos el contexto
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // ACT
            var result = _controller.Error();

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.False(string.IsNullOrEmpty(model.RequestId));
        }
    }
}