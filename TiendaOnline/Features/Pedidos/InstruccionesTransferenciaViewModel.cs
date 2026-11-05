namespace TiendaOnline.Features.Pedidos
{
    public class InstruccionesTransferenciaViewModel
    {
        public int PedidoId { get; set; }
        public string NumeroPedido => PedidoId.ToString("D6");

        public decimal Total { get; set; }

        // Datos de la cuenta para transferir (AppSettings, editables desde el panel)
        public string Cbu { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;

        public string? ComprobanteUrl { get; set; }
        public bool PuedeSubirComprobante { get; set; }
        public bool Vencido { get; set; }
    }
}
