using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;

namespace TiendaOnline.Features.Pagos
{
    [ApiController]
    [Route("api/pagos")]
    public class PagosWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PagosWebhookController> _logger;
        private readonly IPedidoCommandService _pedidoCommandService;

        public PagosWebhookController(IPaymentService paymentService, ILogger<PagosWebhookController> logger, IPedidoCommandService pedidoCommandService)
        {
            _paymentService = paymentService;
            _logger = logger;
            _pedidoCommandService = pedidoCommandService;
        }

        [HttpPost("mercadopago")]
        [AllowAnonymous]
        public async Task<IActionResult> MercadoPagoWebhook([FromQuery] string? topic, [FromQuery(Name = "id")] string? resourceId, [FromBody] MercadoPagoWebhookDto? body)
        {
            // Unificamos el ID: Puede venir por Query String (id) o por el Body (data.id)
            var finalId = resourceId ?? body?.Data?.Id;

            // Unificamos el Tipo: Puede venir como 'topic' (v1) o como 'type' (v2/JSON body)
            var finalType = topic ?? body?.Type;

            // --- VALIDACIÓN DE SEGURIDAD (Firma de Mercado Pago) ---
            // Obtenemos los encabezados necesarios para validar que la petición sea auténtica
            var signature = Request.Headers["x-signature"].ToString();
            var requestId = Request.Headers["x-request-id"].ToString();

            // Validamos la firma usando el servicio (importante tener cargado el WebhookSecret en appsettings)
            if (!_paymentService.ValidarFirma(signature, requestId, finalId, finalType))
            {
                _logger.LogWarning("Firma de Webhook inválida o ausente para el recurso {Id}. Posible intento de fraude.", finalId);
                // Devolvemos Unauthorized para que quede registro de que la firma falló
                return Unauthorized();
            }

            _logger.LogInformation("Webhook recibido de MP y validado: Tipo/Topic {Type}, ResourceId {Id}", finalType, finalId);

            // Validamos que tengamos un ID para trabajar
            if (string.IsNullOrEmpty(finalId))
            {
                _logger.LogWarning("Se recibió una notificación de MP sin ID de recurso.");
                return Ok();
            }

            // Procesamos solo si es un pago (payment)
            if (finalType == "payment" || string.IsNullOrEmpty(finalType))
            {
                // Le pedimos al servicio de MP que traiga la data real usando el ID unificado
                var infoPago = await _paymentService.ObtenerDetallesPagoAsync(finalId);

                // Si MP nos devolvió data y el pago está aprobado
                if (infoPago != null && infoPago.Estado == "approved")
                {
                    await _pedidoCommandService.ConfirmarPagoAsync(infoPago);
                    _logger.LogInformation("Pago {Id} confirmado y pedido actualizado.", finalId);
                }
            }

            // SIEMPRE responder 200 a MP para evitar reintentos infinitos
            return Ok();
        }
    }
}
