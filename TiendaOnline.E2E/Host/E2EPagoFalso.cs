using System.Collections.Concurrent;
using TiendaOnline.Application.Payment;

namespace TiendaOnline.E2E.Host
{
    /// <summary>
    /// Tramo de MercadoPago simulado para los smoke tests E2E. Cuando el checkout pide la
    /// preferencia se registra el monto exacto del pedido (items + envío) y el
    /// InitPoint que devuelve es el return a PagosExitoso con payment_id: es lo que
    /// hace la web real de MercadoPago cuando el pago aprobado vuelve. El tramo
    /// externo de MP (formularios de pago, tarjetas) queda fuera del alcance: es el
    /// mundo de MP, no el de la tienda. El monto que reporta matchea el total que
    /// valida ConfirmarPagoAsync, así el circuito de reserva de stock y emails
    /// corre igual que en producción.
    /// </summary>
    public class E2EPagoFalso : IPaymentService
    {
        private sealed record PagoRegistrado(int PedidoId, decimal Monto);

        private readonly ConcurrentDictionary<string, PagoRegistrado> _pagos = new();

        public Task<string> GenerarPreferenciaPagoAsync(PedidoPagoDto pedidoDto, CancellationToken ct = default)
        {
            var monto = pedidoDto.Items.Sum(i => i.Cantidad * i.PrecioUnitario) + pedidoDto.CostoEnvio;
            var paymentId = $"e2e-{pedidoDto.PedidoId}-{Guid.NewGuid():N}";
            _pagos[paymentId] = new PagoRegistrado(pedidoDto.PedidoId, monto);

            // URL relativa: el browser la resuelve contra el origen del host E2E.
            return Task.FromResult($"/Pedidos/PagoExitoso?external_reference={pedidoDto.PedidoId}&payment_id={paymentId}");
        }

        public Task<InfoPagoDto?> ObtenerDetallesPagoAsync(string paymentId, CancellationToken ct = default)
        {
            if (!_pagos.TryGetValue(paymentId, out var pago))
            {
                return Task.FromResult<InfoPagoDto?>(null);
            }

            return Task.FromResult<InfoPagoDto?>(new InfoPagoDto
            {
                PedidoId = pago.PedidoId,
                MontoPagado = pago.Monto,
                Estado = "approved",
                TransaccionId = paymentId
            });
        }

        public bool ValidarFirma(string signatureHeader, string requestId, string resourceId, string topic) => true;
    }
}
