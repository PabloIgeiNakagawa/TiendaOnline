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
        public async Task<IActionResult> MercadoPagoWebhook([FromQuery] string topic, [FromQuery(Name = "id")] string resourceId)
        {
            // MP envía notificaciones de varios tipos. Nos interesa "payment".
            if (topic == "payment" || string.IsNullOrEmpty(topic))
            {
                _logger.LogInformation("Webhook recibido de MP: Topic {Topic}, ResourceId {Id}", topic, resourceId);

                // Si el ID es nulo, MP a veces envía pruebas de conexión
                if (string.IsNullOrEmpty(resourceId)) return Ok();

                // Le pedimos al servicio de MP que traiga la data
                var infoPago = await _paymentService.ObtenerDetallesPagoAsync(resourceId);

                // Si MP nos devolvió data y el pago está aprobado, actuamos en nuestra DB
                if (infoPago != null && infoPago.Estado == "approved")
                {
                    await _pedidoCommandService.ConfirmarPagoAsync(infoPago);
                }
            }

            return Ok(); // Siempre responder 200/201 a MP para evitar reintentos infinitos
        }
    }
}
