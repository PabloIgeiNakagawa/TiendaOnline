namespace TiendaOnline.Application.Productos.Commands
{
    public class AgregarProductoDto
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int CategoriaId { get; set; }
        public Stream ImagenStream { get; set; }
        public string NombreArchivo { get; set; }
    }
}
