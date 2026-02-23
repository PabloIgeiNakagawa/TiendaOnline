using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Tienda.Productos
{
    public interface IProductoService
    {
        Task<Producto?> ObtenerProductoAsync(int id);
        Task<List<Producto>> ObtenerProductosAsync();
        Task<PagedResult<ProductoDto>> ObtenerProductosTiendaPaginadoAsync(string busqueda, int? categoriaId, decimal? min, decimal? max, string orden, int pagina, int cantidad);
    }
}
