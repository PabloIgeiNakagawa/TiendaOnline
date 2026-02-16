using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Categoria;

namespace TiendaOnline.Areas.Admin.ViewModels.Categoria
{
    public class CategoriaListadoViewModel
    {
        public PagedResult<CategoriaListadoDto> Paginacion { get; set; }
        public string? Busqueda { get; set; }
        public string? NivelSeleccionado { get; set; }
    }
}
