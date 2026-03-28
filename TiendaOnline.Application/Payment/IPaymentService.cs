namespace TiendaOnline.Application.Payment
{
    public interface IPaymentService
    {
        // Retorna la URL de redirección(InitPoint)
        Task<string> GenerarPreferenciaPagoAsync(PedidoPagoDto pedidoDto);
        Task<InfoPagoDto?> ObtenerDetallesPagoAsync(string paymentId);
    }
}
