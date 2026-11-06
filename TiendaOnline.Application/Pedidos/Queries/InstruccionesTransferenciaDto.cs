namespace TiendaOnline.Application.Pedidos.Queries
{
    /// <summary>
    /// Datos que armamos del lado servidor para la página de instrucciones de transferencia.
    /// </summary>
    public class InstruccionesTransferenciaDto
    {
        public int PedidoId { get; set; }

        public decimal Total { get; set; }

        public string? ComprobanteUrl { get; set; }

        // Solo verdadero si el pedido sigue esperando el pago y todavía no hay comprobante adjunto.
        public bool PuedeSubirComprobante { get; set; }

        public bool Vencido { get; set; }
    }
}
