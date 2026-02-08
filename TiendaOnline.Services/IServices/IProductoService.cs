using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Producto;

namespace TiendaOnline.Services.IServices
{
    public interface IProductoService
    {
        Task<Producto?> ObtenerProductoAsync(int id);
        Task<List<Producto>> ObtenerProductosAsync();
        Task<PagedResult<ProductoListaDto>> ObtenerProductosPaginadosAsync(string? busqueda, int? categoriaId, string? estado, string? stock, int pagina, int registrosPorPagina);
        Task AgregarProductoAsync(Producto producto);
        Task DarBajaProductoAsync(int productoId);
        Task DarAltaProductoAsync(int productoId);
        Task EditarProductoAsync(int productoId, Producto productoEditado, Stream archivoStream, string nombreArchivo);
    }
}
