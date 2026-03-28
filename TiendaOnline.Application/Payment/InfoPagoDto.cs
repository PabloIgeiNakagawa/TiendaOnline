namespace TiendaOnline.Application.Payment
{
    public class InfoPagoDto
    {
        public int PedidoId { get; set; }
        public decimal MontoPagado { get; set; }
        public string Estado { get; set; }
        public string TransaccionId { get; set; }
    }
}
