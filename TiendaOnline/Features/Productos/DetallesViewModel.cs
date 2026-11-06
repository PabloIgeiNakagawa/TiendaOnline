namespace TiendaOnline.Features.Productos
{
    public class DetallesViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; } // precio de lista
        public decimal? PrecioOferta { get; set; } // oferta vigente si hay; el precio grande de la ficha es PrecioOferta ?? Precio
        public DateTime? FechaFinOferta { get; set; }
        public int Stock { get; set; }
        public string ImagenUrl { get; set; }
        public string CategoriaNombre { get; set; }
        public int CategoriaId { get; set; }
    }
}
