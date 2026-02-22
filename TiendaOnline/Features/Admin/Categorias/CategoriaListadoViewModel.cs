using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Admin.Categorias
{
    public class CategoriaListadoViewModel
    {
        public PagedResult<CategoriaListadoDto> Paginacion { get; set; }
        public string? Busqueda { get; set; }
        public string? NivelSeleccionado { get; set; }
    }
}
