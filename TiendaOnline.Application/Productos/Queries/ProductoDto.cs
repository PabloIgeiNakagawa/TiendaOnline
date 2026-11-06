namespace TiendaOnline.Application.Productos.Queries
{
    public class ProductoDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; } // precio de lista
        public decimal? PrecioOferta { get; set; } // oferta vigente si hay; el precio efectivo es PrecioOferta ?? Precio
        public DateTime? FechaFinOferta { get; set; } // hasta cuándo vale la oferta (si la hay)
        public int Stock { get; set; }
        public string ImagenUrl { get; set; }
        public string CategoriaNombre { get; set; }
        public int CategoriaId { get; set; }
    }
}
