using Moq;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Infrastructure.Services.Carritos;

namespace TiendaOnline.Tests.Services.Carritos
{
    /// <summary>
    /// Tests de CarritoService: unit puros sobre mocks de ICarritoStorage e
    /// IProductoQueryService (sin DB ni HTTP/session).
    /// Comportamientos documentados acá: agregar inicia en 1 y valida stock contra la
    /// BD; los caminos de fallo no persisten cambios (Times.Never sobre GuardarAsync);
    /// ValidarStock refresca StockDisponible y Precio desde la BD y persiste (ver
    /// TECH_DEBT #6 precio stale). ActualizarCantidad es distinto:
    /// usa el StockDisponible de la sesión, no consulta BD.
    /// </summary>
    public class CarritoServiceTests
    {
        // ------------------------------------------------------------------
        // Helpers de montaje
        // ------------------------------------------------------------------

        private static ProductoDto Producto(int productoId, decimal precio = 100m, int stock = 5,
            string nombre = "Prod")
            => new()
            {
                ProductoId = productoId,
                Nombre = nombre,
                Descripcion = "Desc",
                Precio = precio,
                Stock = stock,
                ImagenUrl = "https://img.test/foto.png",
                CategoriaNombre = "Cat",
                CategoriaId = 1
            };

        private static Mock<IProductoQueryService> MockProducto(int productoId, ProductoDto? producto)
        {
            var mock = new Mock<IProductoQueryService>();
            mock.Setup(x => x.ObtenerProductoAsync(productoId)).ReturnsAsync(producto!);
            return mock;
        }

        private static Mock<ICarritoStorage> MockStorage(params ItemCarrito[] items)
        {
            var mock = new Mock<ICarritoStorage>();
            mock.Setup(x => x.ObtenerAsync()).ReturnsAsync(items.ToList());
            return mock;
        }

        private static (Mock<ICarritoStorage> Storage, List<ItemCarrito> Guardado) StorageConCaptura(
            params ItemCarrito[] items)
        {
            var guardado = new List<ItemCarrito>();
            var mock = new Mock<ICarritoStorage>();
            mock.Setup(x => x.ObtenerAsync()).ReturnsAsync(items.ToList());
            mock.Setup(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()))
                .Callback<List<ItemCarrito>>(g => guardado.AddRange(g))
                .Returns(Task.CompletedTask);
            return (mock, guardado);
        }

        // ------------------------------------------------------------------
        // Agregar
        // ------------------------------------------------------------------

        [Fact]
        public async Task Agregar_ProductoInexistente_RetornaFailureYNoGuarda()
        {
            var storage = MockStorage();
            var sut = new CarritoService(storage.Object, MockProducto(1, null).Object);

            var resultado = await sut.AgregarAsync(1);

            Assert.False(resultado.IsSuccess);
            Assert.Equal("Producto no encontrado.", resultado.Error);
            storage.Verify(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()), Times.Never);
        }

        [Fact]
        public async Task Agregar_StockCero_RetornaFailureYNoGuarda()
        {
            var storage = MockStorage();
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 0)).Object);

            var resultado = await sut.AgregarAsync(1);

            Assert.False(resultado.IsSuccess);
            Assert.Contains("No hay stock disponible", resultado.Error);
            storage.Verify(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()), Times.Never);
        }

        [Fact]
        public async Task Agregar_ProductoNuevo_RetornaSuccess_EmpiezaEnUnaConDatosDelProducto()
        {
            var (storage, guardado) = StorageConCaptura();
            var sut = new CarritoService(storage.Object, MockProducto(7, Producto(7, precio: 99.5m, stock: 4)).Object);

            var resultado = await sut.AgregarAsync(7);

            Assert.True(resultado.IsSuccess);
            var item = Assert.Single(guardado);
            Assert.Equal(7, item.ProductoId);
            Assert.Equal(1, item.Cantidad);
            Assert.Equal(99.5m, item.Precio);
            Assert.Equal("https://img.test/foto.png", item.ImagenUrl);
            Assert.Equal(4, item.StockDisponible);
        }

        [Fact]
        public async Task Agregar_ItemExistente_IncrementaCantidadYGuarda()
        {
            var existente = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 1, StockDisponible = 5 };
            var (storage, guardado) = StorageConCaptura(existente);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 5)).Object);

            var resultado = await sut.AgregarAsync(1);

            Assert.True(resultado.IsSuccess);
            Assert.Equal(2, Assert.Single(guardado).Cantidad);
        }

        [Fact]
        public async Task Agregar_ItemExistente_AlMaximoDelStock_RetornaFailureYNoGuarda()
        {
            var existente = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 5, StockDisponible = 5 };
            var storage = MockStorage(existente);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 5)).Object);

            var resultado = await sut.AgregarAsync(1);

            Assert.False(resultado.IsSuccess);
            Assert.Contains("No hay más stock disponible", resultado.Error);
            storage.Verify(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()), Times.Never);
        }

        // ------------------------------------------------------------------
        // ActualizarCantidad
        // ------------------------------------------------------------------

        [Fact]
        public async Task ActualizarCantidad_ProductoAusenteEnCarrito_RetornaFailureYNoGuarda()
        {
            var storage = MockStorage();
            // Tampoco consulta la BD: el producto ya no está en el carrito de sesión.
            var sut = new CarritoService(storage.Object, new Mock<IProductoQueryService>().Object);

            var resultado = await sut.ActualizarCantidadAsync(1, 2);

            Assert.False(resultado.IsSuccess);
            Assert.Contains("no encontrado en el carrito", resultado.Error);
            storage.Verify(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarCantidad_MayorQueStockDisponible_RetornaFailureYNoGuarda()
        {
            var item = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 1, StockDisponible = 5 };
            var storage = MockStorage(item);
            // Nota (comportamiento de hoy): ActualizarCantidad usa el StockDisponible
            // del item de sesión, no consulta la BD (a diferencia de Agregar).
            var sut = new CarritoService(storage.Object, new Mock<IProductoQueryService>().Object);

            var resultado = await sut.ActualizarCantidadAsync(1, 6);

            Assert.False(resultado.IsSuccess);
            Assert.Contains("Solo hay 5 unidades disponibles", resultado.Error);
            storage.Verify(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarCantidad_DentroDelStock_RetornaSuccessYGuarda()
        {
            var item = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 1, StockDisponible = 5 };
            var (storage, guardado) = StorageConCaptura(item);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 5)).Object);

            var resultado = await sut.ActualizarCantidadAsync(1, 3);

            Assert.True(resultado.IsSuccess);
            Assert.Equal(3, Assert.Single(guardado).Cantidad);
        }

        // ------------------------------------------------------------------
        // Eliminar
        // ------------------------------------------------------------------

        [Fact]
        public async Task Eliminar_ProductoNoEstaEnCarrito_RetornaSuccessYGuardaIgual()
        {
            // Documentado: eliminar un producto ausente devuelve success y persiste
            // el carrito de todas formas (así está escrito CarritoService.EliminarAsync hoy).
            var (storage, _) = StorageConCaptura();
            var sut = new CarritoService(storage.Object, MockProducto(1, null).Object);

            var resultado = await sut.EliminarAsync(1);

            Assert.True(resultado.IsSuccess);
            storage.Verify(x => x.GuardarAsync(It.IsAny<List<ItemCarrito>>()), Times.Once);
        }

        [Fact]
        public async Task Eliminar_ProductoPresente_LoQuitaYGuarda()
        {
            var enCarrito = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 2, StockDisponible = 5 };
            var (storage, guardado) = StorageConCaptura(enCarrito);
            var sut = new CarritoService(storage.Object, MockProducto(1, null).Object);

            var resultado = await sut.EliminarAsync(1);

            Assert.True(resultado.IsSuccess);
            Assert.Empty(guardado);
        }

        // ------------------------------------------------------------------
        // ValidarStock
        // ------------------------------------------------------------------

        [Fact]
        public async Task ValidarStock_ProductoEliminadoDeLaBase_EntraAItemsSinStock()
        {
            // Documentado: ValidarStock SIEMPRE devuelve Result success; el flag
            // de negocio (TodoOK) es el que indica si el carrito es comprable.
            var enCarrito = new ItemCarrito { ProductoId = 1, Nombre = "Prod muerto", Precio = 100m, Cantidad = 3 };
            var (storage, _) = StorageConCaptura(enCarrito);
            var sut = new CarritoService(storage.Object, MockProducto(1, null).Object);

            var resultado = await sut.ValidarStockAsync();

            Assert.True(resultado.IsSuccess);
            Assert.False(resultado.Value.TodoOK);
            var sinStock = Assert.Single(resultado.Value.ItemsSinStock);
            Assert.Equal(1, sinStock.ProductoId);
            Assert.Equal(0, sinStock.StockDisponible);
            Assert.Equal(3, sinStock.CantidadSolicitada);
        }

        [Fact]
        public async Task ValidarStock_StockInsuficiente_DevuelveItemsConDatos()
        {
            var enCarrito = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 3, StockDisponible = 1 };
            var (storage, _) = StorageConCaptura(enCarrito);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 1)).Object);

            var resultado = await sut.ValidarStockAsync();

            Assert.True(resultado.IsSuccess);
            Assert.False(resultado.Value.TodoOK);
            var sinStock = Assert.Single(resultado.Value.ItemsSinStock);
            Assert.Equal(3, sinStock.CantidadSolicitada);
            Assert.Equal(1, sinStock.StockDisponible); // stock real de BD (el refresco del item en carrito da igual para el Dto)
        }

        [Fact]
        public async Task ValidarStock_RefrescaStockDisponibleDesdeBD_Persiste()
        {
            // Clave para el checkout: el carrito viaja con StockDisponible stale
            // (session vieja); ValidarStock lo recalcula contra BD y persiste.
            var enCarrito = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 1, StockDisponible = 99 };
            var (storage, guardado) = StorageConCaptura(enCarrito);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 2)).Object);

            var resultado = await sut.ValidarStockAsync();

            Assert.True(resultado.Value.TodoOK);
            Assert.Equal(2, Assert.Single(guardado).StockDisponible); // 99 stale → 2 real
        }

        [Fact]
        public async Task ValidarStock_RefrescaPrecioDesdeBD_Persiste()
        {
            // TECH_DEBT #6: el precio del carrito es un snapshot; si cambió en la BD
            // (aumento, oferta futura), ValidarStock deja el vigente en el item.
            var enCarrito = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 2, StockDisponible = 5 };
            var (storage, guardado) = StorageConCaptura(enCarrito);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, precio: 89.9m, stock: 5)).Object);

            var resultado = await sut.ValidarStockAsync();

            Assert.True(resultado.Value.TodoOK);
            var item = Assert.Single(guardado);
            Assert.Equal(89.9m, item.Precio); // 100 stale → 89.9 vigente en BD
            Assert.Equal(2, item.Cantidad);   // el refresco no toca la cantidad elegida
        }

        [Fact]
        public async Task ValidarStock_TodoOK_RetornaItemsSinStockVacios()
        {
            var enCarrito = new ItemCarrito { ProductoId = 1, Nombre = "Prod", Precio = 100m, Cantidad = 3, StockDisponible = 5 };
            var (storage, _) = StorageConCaptura(enCarrito);
            var sut = new CarritoService(storage.Object, MockProducto(1, Producto(1, stock: 5)).Object);

            var resultado = await sut.ValidarStockAsync();

            Assert.True(resultado.Value.TodoOK);
            Assert.Empty(resultado.Value.ItemsSinStock);
        }

        // ------------------------------------------------------------------
        // Lecturas / vaciado
        // ------------------------------------------------------------------

        [Fact]
        public async Task ObtenerCantidadTotal_SumaLasCantidades()
        {
            var storage = MockStorage(
                new ItemCarrito { ProductoId = 1, Cantidad = 2 },
                new ItemCarrito { ProductoId = 2, Cantidad = 3 });
            var sut = new CarritoService(storage.Object, MockProducto(1, null).Object);

            var total = await sut.ObtenerCantidadTotalAsync();

            Assert.Equal(5, total);
        }

        [Fact]
        public async Task Vaciar_LlamaALimpiarDelStorage()
        {
            var storage = MockStorage();
            var sut = new CarritoService(storage.Object, MockProducto(1, null).Object);

            await sut.VaciarAsync();

            storage.Verify(x => x.LimpiarAsync(), Times.Once);
        }
    }
}
