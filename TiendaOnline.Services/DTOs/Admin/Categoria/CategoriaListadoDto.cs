namespace TiendaOnline.Services.DTOs.Admin.Categoria
{
    public class CategoriaListadoDto
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; }
        public int? CategoriaPadreId { get; set; }
        public string? CategoriaPadreNombre { get; set; }
        public int CantidadProductos { get; set; }
        public int CantidadSubcategorias { get; set; }
        public bool Activa { get; set; }
    }
}
