namespace TiendaOnline.Application.Payment
{
    public interface IPaymentService
    {
        // Retorna la URL de redirección(InitPoint)
        // El SDK de MercadoPago no expone CancellationToken en sus llamadas HTTP: ct queda listo para cuando el SDK lo soporte.
        Task<string> GenerarPreferenciaPagoAsync(PedidoPagoDto pedidoDto, CancellationToken ct = default);
        Task<InfoPagoDto?> ObtenerDetallesPagoAsync(string paymentId, CancellationToken ct = default);
        bool ValidarFirma(string signatureHeader, string requestId, string resourceId, string topic);
    }
}
