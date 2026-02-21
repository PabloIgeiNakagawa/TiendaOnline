using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs;

namespace TiendaOnline.Features.Tienda.Productos
{
    public class ProductoIndexViewModel
    {
        public PagedResult<ProductoDto> Paginacion { get; set; }

        // Filtros
        public string Busqueda { get; set; }
        public int? CategoriaId { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }
        public string Orden { get; set; }

        // Datos para los componentes de la vista
        public List<Categoria> CategoriasRaiz { get; set; }
    }
}
