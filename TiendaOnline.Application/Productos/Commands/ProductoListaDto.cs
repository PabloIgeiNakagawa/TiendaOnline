namespace TiendaOnline.Application.Productos.Commands
{
    public class ProductoListaDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? ImagenUrl { get; set; }
        public bool Activo { get; set; }
        public string CategoriaNombre { get; set; } = "Sin categoría";
        public int? CategoriaId { get; set; }
    }
}
