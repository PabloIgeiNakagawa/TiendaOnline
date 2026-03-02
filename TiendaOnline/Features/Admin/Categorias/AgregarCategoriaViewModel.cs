namespace TiendaOnline.Features.Admin.Categorias
{
    public class AgregarCategoriaViewModel
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int? CategoriaPadreId { get; set; }
    }
}
