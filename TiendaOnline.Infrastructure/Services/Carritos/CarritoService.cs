using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Productos.Queries;

namespace TiendaOnline.Infrastructure.Services.Carritos
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

        public async Task<Result> AgregarAsync(int productoId)
        {
            var carrito = await _storage.ObtenerAsync();
            var producto = await _productoQueryService.ObtenerProductoAsync(productoId);

            if (producto == null)
                return Result.Failure("Producto no encontrado.");

            if (producto.Stock <= 0)
                return Result.Failure($"No hay stock disponible para {producto.Nombre}.");

            var existente = carrito.FirstOrDefault(x => x.ProductoId == productoId);

            if (existente != null)
            {
                if (existente.Cantidad >= producto.Stock)
                    return Result.Failure($"No hay más stock disponible para {producto.Nombre}.");

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
                    Cantidad = 1,
                    StockDisponible = producto.Stock
                });
            }

            await _storage.GuardarAsync(carrito);
            return Result.Success();
        }

        public async Task<Result> EliminarAsync(int productoId)
        {
            var carrito = await _storage.ObtenerAsync();
            var item = carrito.FirstOrDefault(x => x.ProductoId == productoId);

            if (item != null)
                carrito.Remove(item);

            await _storage.GuardarAsync(carrito);
            return Result.Success();
        }

        public async Task<Result> ActualizarCantidadAsync(int productoId, int cantidad)
        {
            var carrito = await _storage.ObtenerAsync();
            var item = carrito.FirstOrDefault(x => x.ProductoId == productoId);

            if (item == null)
                return Result.Failure("Producto no encontrado en el carrito.");

            if (cantidad > item.StockDisponible)
                return Result.Failure($"Solo hay {item.StockDisponible} unidades disponibles de {item.Nombre}.");

            item.Cantidad = cantidad;

            await _storage.GuardarAsync(carrito);
            return Result.Success();
        }

        public async Task<Result<ValidacionStockDto>> ValidarStockAsync()
        {
            var carrito = await _storage.ObtenerAsync();
            var itemsSinStock = new List<StockInsuficienteDto>();

            foreach (var item in carrito)
            {
                var producto = await _productoQueryService.ObtenerProductoAsync(item.ProductoId);
                if (producto == null)
                {
                    itemsSinStock.Add(new StockInsuficienteDto
                    {
                        ProductoId = item.ProductoId,
                        Nombre = item.Nombre,
                        CantidadSolicitada = item.Cantidad,
                        StockDisponible = 0
                    });
                    continue;
                }

                if (producto.Stock < item.Cantidad)
                {
                    itemsSinStock.Add(new StockInsuficienteDto
                    {
                        ProductoId = item.ProductoId,
                        Nombre = item.Nombre,
                        CantidadSolicitada = item.Cantidad,
                        StockDisponible = producto.Stock
                    });
                }

                item.StockDisponible = producto.Stock;
            }

            await _storage.GuardarAsync(carrito);

            var resultado = new ValidacionStockDto
            {
                TodoOK = !itemsSinStock.Any(),
                ItemsSinStock = itemsSinStock
            };

            return Result<ValidacionStockDto>.Success(resultado);
        }

        public async Task VaciarAsync()
        {
            await _storage.LimpiarAsync();
        }
    }
}
