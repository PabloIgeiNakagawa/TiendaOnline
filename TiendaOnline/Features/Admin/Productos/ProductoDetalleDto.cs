namespace TiendaOnline.Features.Admin.Productos
{
    public class ProductoDetalleDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int CategoriaId { get; set; }
        public string? NombreCategoria { get; set; } // Lo traemos del Include
        public string? ImagenUrl { get; set; }
    }
}
