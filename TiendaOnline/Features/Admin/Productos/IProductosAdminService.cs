using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Admin.Productos
{
    public interface IProductosAdminService
    {
        Task<ProductoDetalleDto?> ObtenerProductoAsync(int id);
        Task<PagedResult<ProductoListaDto>> ObtenerProductosPaginadosAsync(string? busqueda, int? categoriaId, string? estado, string? stock, int pagina, int registrosPorPagina);
        Task AgregarProductoAsync(AgregarProductoDto dto);
        Task DarAltaProductoAsync(int productoId);
        Task DarBajaProductoAsync(int productoId);
        Task EditarProductoAsync(EditarProductoDto dto);
    }
}
