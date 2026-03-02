using TiendaOnline.Application.Productos.Queries;

namespace TiendaOnline.Application.Carritos
{
    public class CarritoService : ICarritoService
    {
        private readonly ICarritoStorage _storage;
        private readonly IProductoQueryService _productoQueryService;

        public CarritoService(ICarritoStorage storage, IProductoQueryService productoQueryService)
        {
            _storage = storage;
            _productoQueryService = productoQueryService;
        }

        public async Task<List<ItemCarrito>> ObtenerAsync()
        {
            return await _storage.ObtenerAsync();
        }

        public async Task<int> ObtenerCantidadTotalAsync()
        {
            var carrito = await _storage.ObtenerAsync();
            return carrito.Sum(x => x.Cantidad);
        }

        public async Task AgregarAsync(int productoId)
        {
            var carrito = await _storage.ObtenerAsync();
            var producto = await _productoQueryService.ObtenerProductoAsync(productoId);

            var existente = carrito.FirstOrDefault(x => x.ProductoId == productoId);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                carrito.Add(new ItemCarrito
                {
                    ProductoId = producto.ProductoId,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    ImagenUrl = producto.ImagenUrl,
                    Cantidad = 1
                });
            }

            await _storage.GuardarAsync(carrito);
        }

        public async Task EliminarAsync(int productoId)
        {
            var carrito = await _storage.ObtenerAsync();
            var item = carrito.FirstOrDefault(x => x.ProductoId == productoId);

            if (item != null)
                carrito.Remove(item);

            await _storage.GuardarAsync(carrito);
        }

        public async Task ActualizarCantidadAsync(int productoId, int cantidad)
        {
            var carrito = await _storage.ObtenerAsync();
            var item = carrito.FirstOrDefault(x => x.ProductoId == productoId);

            if (item != null)
                item.Cantidad = cantidad;

            await _storage.GuardarAsync(carrito);
        }

        public async Task VaciarAsync()
        {
            await _storage.LimpiarAsync();
        }
    }
}
