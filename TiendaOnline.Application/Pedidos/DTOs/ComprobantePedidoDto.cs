namespace TiendaOnline.Application.Pedidos.DTOs
{
    public record ComprobantePedidoDto(
        int PedidoId,
        string NumeroComprobante,
        DateTime FechaEmision,
        string UsuarioNombre,
        string UsuarioEmail,
        string UsuarioTelefono,
        string? DireccionCompleta,
        string MetodoPago,
        int EstadoPagoId,
        string? TransaccionPagoId,
        List<ComprobanteItemDto> Items,
        decimal Subtotal,
        decimal CostoEnvio,
        decimal Total
    );

    public record ComprobanteItemDto(
        string ProductoNombre,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal
    );
}
