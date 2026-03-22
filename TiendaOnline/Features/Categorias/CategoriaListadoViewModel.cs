using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Common;

namespace TiendaOnline.Features.Categorias
{
    public class CategoriaListadoViewModel
    {
        public PagedResult<CategoriaListadoDto> Paginacion { get; set; }
        public string? Busqueda { get; set; }
        public string? Nivel { get; set; }
    }
}
