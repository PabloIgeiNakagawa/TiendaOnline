using TiendaOnline.Features.Admin.Categorias;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Producto;

namespace TiendaOnline.Features.Admin.Productos
{
    public class ProductoCatalogoViewModel
    {
        public PagedResult<ProductoListaDto> ProductosPaginados { get; set; }
            = new PagedResult<ProductoListaDto>(new List<ProductoListaDto>(), 0, 1, 10);

        // Para los filtros
        public List<CategoriaDto> Categorias { get; set; } = new List<CategoriaDto>();

        // Estado de los filtros (para mantenerlos al recargar)
        public string? Busqueda { get; set; }
        public int? CategoriaSeleccionada { get; set; }
        public string? EstadoSeleccionado { get; set; }
        public string? StockSeleccionado { get; set; }

        // Totales para los badges
        public int TotalActivos { get; set; }
        public int TotalInactivos { get; set; }
    }
}
