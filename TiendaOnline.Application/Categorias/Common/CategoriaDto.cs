namespace TiendaOnline.Application.Categorias.Common
{
    public class CategoriaDto
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int? CategoriaPadreId { get; set; }
        public CategoriaDto? CategoriaPadre { get; set; }
        public virtual ICollection<CategoriaDto> Subcategorias { get; set; } = new List<CategoriaDto>();
    }
}
