using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Categoria;

namespace TiendaOnline.Features.Admin.Categorias
{
    public class CategoriaListadoViewModel
    {
        public PagedResult<CategoriaListadoDto> Paginacion { get; set; }
        public string? Busqueda { get; set; }
        public string? NivelSeleccionado { get; set; }
    }
}
