namespace TiendaOnline.Features.Admin.Productos
{
    public class EditarProductoDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int CategoriaId { get; set; }
        public Stream? ImagenStream { get; set; }
        public string? NombreArchivo { get; set; }
    }
}
