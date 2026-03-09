namespace TiendaOnline.Application.Payment
{
    public class PedidoPagoDto
    {
        public int PedidoId { get; set; }
        public string EmailUsuario { get; set; }
        public List<ItemPagoDto> Items { get; set; } = new();
        public decimal CostoEnvio { get; set; }
    }

    public class ItemPagoDto
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}
