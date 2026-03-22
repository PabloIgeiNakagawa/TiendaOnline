using TiendaOnline.Application.Common;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Application.Categorias.Common;

namespace TiendaOnline.Features.Productos.Admin
{
    public class ProductoCatalogoViewModel
    {
        public PagedResult<ProductoListaDto> ProductosPaginados { get; set; }
            = new PagedResult<ProductoListaDto>(new List<ProductoListaDto>(), 0, 1, 10);

        // Para los filtros
        public List<CategoriaDto> Categorias { get; set; } = new List<CategoriaDto>();

        // Estado de los filtros (para mantenerlos al recargar)
        public string? Busqueda { get; set; }
        public int? CategoriaId { get; set; }
        public string? Estado { get; set; }
        public string? Stock{ get; set; }

        // Totales para los badges
        public int TotalActivos { get; set; }
        public int TotalInactivos { get; set; }
    }
}
