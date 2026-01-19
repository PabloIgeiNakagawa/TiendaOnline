namespace TiendaOnline.Models
{
    public class ProductoDescuento
    {
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        public int DescuentoId { get; set; }
        public Descuento Descuento { get; set; }
    }
}
