using TiendaOnline.Models;

namespace TiendaOnline.IServices
{
    public interface IProductoService
    {
        Task<Producto?> ObtenerProductoAsync(int id);
        Task<List<Producto>> ObtenerProductosAsync();
        Task AgregarProductoAsync(Producto producto);
        Task DarBajaProductoAsync(int productoId);
        Task DarAltaProductoAsync(int productoId);
        Task EditarProductoAsync(int productoId, Producto productoEditado, IFormFile ImagenArchivo);
    }
}
