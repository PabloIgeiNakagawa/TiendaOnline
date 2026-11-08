namespace TiendaOnline.Application.Pedidos.Commands
{
    /// <summary>
    /// El circuito manual de transferencia bancaria: subida del comprobante,
    /// aprobación (misma maquinaria de reserva de stock que MercadoPago) y
    /// rechazo con baja del archivo.
    /// </summary>
    public interface IGestionTransferenciaService
    {
        Task<string> GuardarComprobanteAsync(int pedidoId, Stream archivo, string nombreArchivo, CancellationToken ct = default);
        Task AprobarPagoTransferenciaAsync(int pedidoId, CancellationToken ct = default);
        Task RechazarComprobanteTransferenciaAsync(int pedidoId, CancellationToken ct = default);
    }
}
