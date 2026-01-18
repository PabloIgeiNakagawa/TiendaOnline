using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Services
{
    public class CloudinaryServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly CloudinaryService _service;

        public CloudinaryServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();

            // Simulamos las keys de configuración
            _mockConfig.Setup(c => c["CloudinarySettings:CloudName"]).Returns("test-cloud");
            _mockConfig.Setup(c => c["CloudinarySettings:ApiKey"]).Returns("12345");
            _mockConfig.Setup(c => c["CloudinarySettings:ApiSecret"]).Returns("abcde");

            _service = new CloudinaryService(_mockConfig.Object);
        }

        #region Tests de Lógica de URL (PublicId)

        [Theory]
        [InlineData("https://res.cloudinary.com/demo/image/upload/v1/productos/pc_gamer.jpg", "productos/pc_gamer")]
        [InlineData("https://res.cloudinary.com/demo/image/upload/v1542/logo.png", "logo")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ExtraerPublicIdDesdeUrl_DebeRetornarIdCorrecto(string url, string esperado)
        {
            // ACT
            var resultado = _service.ExtraerPublicIdDesdeUrl(url);

            // ASSERT
            Assert.Equal(esperado, resultado);
        }

        #endregion

        #region Tests de Validación de Archivos

        [Fact]
        public async Task SubirImagenAsync_DebeRetornarNull_SiArchivoEsNulo()
        {
            // ACT
            var resultado = await _service.SubirImagenAsync(null);

            // ASSERT
            Assert.Null(resultado);
        }

        [Fact]
        public async Task SubirImagenAsync_DebeRetornarNull_SiArchivoEstaVacio()
        {
            // ARRANGE
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            // ACT
            var resultado = await _service.SubirImagenAsync(fileMock.Object);

            // ASSERT
            Assert.Null(resultado);
        }

        #endregion

        [Fact]
        public async Task BorrarImagenAsync_DebeRetornarFalso_SiPublicIdEsNulo()
        {
            // ACT
            var resultado = await _service.BorrarImagenAsync(null);

            // ASSERT
            Assert.False(resultado);
        }
    }
}