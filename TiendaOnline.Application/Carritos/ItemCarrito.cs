namespace TiendaOnline.Application.Carritos
{
    public class ItemCarrito
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public decimal PrecioLista { get; set; } // precio de lista para tachar en la UI; 0 = dato ausente. Precio = precio efectivo (oferta vigente ?? lista).
        public int Cantidad { get; set; }
        public string? ImagenUrl { get; set; }
        public int StockDisponible { get; set; }
        public bool HayStockSuficiente => StockDisponible >= Cantidad;
        public bool SinStock => StockDisponible == 0;
    }

}
