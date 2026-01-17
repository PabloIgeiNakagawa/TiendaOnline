using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Services
{
    public class ProductoServiceTests : IDisposable
    {
        private readonly TiendaContext _db;
        private readonly Mock<IAuditoriaService> _mockAuditoria;
        private readonly Mock<IImagenService> _mockImagen;
        private readonly ProductoService _service;

        public ProductoServiceTests()
        {
            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TiendaContext(options);
            _mockAuditoria = new Mock<IAuditoriaService>();
            _mockImagen = new Mock<IImagenService>();

            _service = new ProductoService(_db, _mockAuditoria.Object, _mockImagen.Object);
        }

        #region Tests de Creación y Alta/Baja

        [Fact]
        public async Task AgregarProductoAsync_DebeGuardarYAuditar()
        {
            // ARRANGE
            var producto = new Producto { Nombre = "Teclado RGB", Descripcion = "Descripción para mouse", Precio = 5000, Stock = 10, CategoriaId = 1 };

            // ACT
            await _service.AgregarProductoAsync(producto);

            // ASSERT
            var enDb = await _db.Productos.FirstOrDefaultAsync(p => p.Nombre == "Teclado RGB");
            Assert.NotNull(enDb);
            _mockAuditoria.Verify(a => a.RegistrarAccionAsync("Agregar Producto", null, It.IsAny<Producto>()), Times.Once);
        }

        [Fact]
        public async Task DarBajaProductoAsync_DebeCambiarEstadoAInactivo()
        {
            // ARRANGE
            var producto = new Producto { ProductoId = 1, Nombre = "Monitor", Descripcion = "Descripción para mouse", Activo = true };
            _db.Productos.Add(producto);
            await _db.SaveChangesAsync();

            // ACT
            await _service.DarBajaProductoAsync(1);

            // ASSERT
            var resultado = await _db.Productos.FindAsync(1);
            Assert.False(resultado.Activo);
            _mockAuditoria.Verify(a => a.RegistrarAccionAsync("Dar de baja producto", It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region Tests de Edición e Imágenes (Mocks)

        [Fact]
        public async Task EditarProductoAsync_DebeReemplazarImagen_SiSeSubeArchivo()
        {
            // ARRANGE
            var productoOriginal = new Producto
            {
                ProductoId = 10,
                Nombre = "Mouse",
                Descripcion = "Descripción para mouse",
                ImagenUrl = "https://res.cloudinary.com/demo/image/upload/v1/vieja.jpg"
            };
            _db.Productos.Add(productoOriginal);
            await _db.SaveChangesAsync();

            // Simulamos el archivo de imagen nuevo
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);

            // Configuramos los Mocks de Imagen
            _mockImagen.Setup(s => s.ExtraerPublicIdDesdeUrl(It.IsAny<string>())).Returns("vieja");
            _mockImagen.Setup(s => s.SubirImagenAsync(It.IsAny<IFormFile>())).ReturnsAsync("https://res.cloudinary.com/demo/image/upload/v1/nueva.jpg");

            var datosEditados = new Producto { Nombre = "Mouse Gamer", Precio = 2000 };

            // ACT
            await _service.EditarProductoAsync(10, datosEditados, fileMock.Object);

            // ASSERT
            var editado = await _db.Productos.FindAsync(10);
            Assert.Equal("https://res.cloudinary.com/demo/image/upload/v1/nueva.jpg", editado.ImagenUrl);

            // Verificamos que se intentó borrar la imagen vieja y subir la nueva
            _mockImagen.Verify(s => s.BorrarImagenAsync("vieja"), Times.Once);
            _mockImagen.Verify(s => s.SubirImagenAsync(fileMock.Object), Times.Once);
        }

        [Fact]
        public async Task EditarProductoAsync_LanzaExcepcion_SiProductoNoExiste()
        {
            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.EditarProductoAsync(999, new Producto(), null));

            Assert.Equal("Producto no encontrado.", ex.Message);
        }

        #endregion

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}