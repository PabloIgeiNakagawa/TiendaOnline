using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.Productos;
using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Tienda.Productos
{
    public interface IProductoService
    {
        Task<Producto?> ObtenerProductoAsync(int id);
        Task<List<Producto>> ObtenerProductosAsync();
        Task<PagedResult<ProductoDto>> ObtenerProductosTiendaPaginadoAsync(string busqueda, int? categoriaId, decimal? min, decimal? max, string orden, int pagina, int cantidad);
        Task<PagedResult<ProductoListaDto>> ObtenerProductosPaginadosAsync(string? busqueda, int? categoriaId, string? estado, string? stock, int pagina, int registrosPorPagina);
        Task AgregarProductoAsync(Producto producto);
        Task DarBajaProductoAsync(int productoId);
        Task DarAltaProductoAsync(int productoId);
        Task EditarProductoAsync(int productoId, Producto productoEditado, Stream archivoStream, string nombreArchivo);
    }
}
