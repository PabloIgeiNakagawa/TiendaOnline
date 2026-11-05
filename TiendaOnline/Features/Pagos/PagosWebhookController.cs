using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;

#if DEBUG
using Microsoft.AspNetCore.Hosting;
#endif

namespace TiendaOnline.Features.Pagos
{
    [ApiController]
    [Route("api/pagos")]
    public class PagosWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PagosWebhookController> _logger;
        private readonly IPedidoCommandService _pedidoCommandService;
#if DEBUG
        private readonly IWebHostEnvironment _environment;

        public PagosWebhookController(IPaymentService paymentService, ILogger<PagosWebhookController> logger, IPedidoCommandService pedidoCommandService, IWebHostEnvironment environment)
        {
            _paymentService = paymentService;
            _logger = logger;
            _pedidoCommandService = pedidoCommandService;
            _environment = environment;
        }
#else
        public PagosWebhookController(IPaymentService paymentService, ILogger<PagosWebhookController> logger, IPedidoCommandService pedidoCommandService)
        {
            _paymentService = paymentService;
            _logger = logger;
            _pedidoCommandService = pedidoCommandService;
        }
#endif

        [HttpPost("mercadopago")]
        [AllowAnonymous]
        public async Task<IActionResult> MercadoPagoWebhook([FromQuery] string? topic, [FromQuery(Name = "id")] string? resourceId, [FromBody] MercadoPagoWebhookDto? body)
        {
            // La petición cruda (payload, headers, firma) se registra en Debug:
            // vale para reproducir un webhook de MP, no es contenido de Info y de Info expone datos de terceros.
            _logger.LogDebug("Webhook MP crudo - topic: {Topic}, id: {ResourceId}, x-signature: {Signature}, x-request-id: {RequestId}, body: {Body}",
                topic, resourceId,
                Request.Headers["x-signature"].ToString(),
                Request.Headers["x-request-id"].ToString(),
                body != null ? JsonSerializer.Serialize(body) : "null");

            // Unificamos el ID: Puede venir por Query String (id) o por el Body (data.id)
            var finalId = resourceId ?? body?.Data?.Id;

            // Unificamos el Tipo: Puede venir como 'topic' (v1) o como 'type' (v2/JSON body)
            var finalType = topic ?? body?.Type;

            _logger.LogInformation("Notificación de MP recibida - Tipo: {Type}, ResourceId: {Id}", finalType, finalId);

            // --- VALIDACIÓN DE SEGURIDAD (Firma de Mercado Pago) ---
            // Obtenemos los encabezados necesarios para validar que la petición sea auténtica
            var signature = Request.Headers["x-signature"].ToString();
            var requestId = Request.Headers["x-request-id"].ToString();

            // Validamos la firma usando el servicio (importante tener cargado el WebhookSecret en appsettings)
            // Usamos string.Empty para los valores nulos - la validación fallará gracefully
            if (!_paymentService.ValidarFirma(signature, requestId, finalId ?? string.Empty, finalType ?? string.Empty))
            {
                _logger.LogWarning("Firma de webhook inválida - Rechazando petición para el recurso {Id}", finalId);
                // Devolvemos Unauthorized para que quede registro de que la firma falló
                return Unauthorized();
            }

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
                var infoPago = await _paymentService.ObtenerDetallesPagoAsync(finalId, HttpContext.RequestAborted);

                if (infoPago != null)
                {
                    // Delegamos todo el procesamiento al servicio, que ya maneja los estados:
                    // approved, rejected, cancelled, refunded, etc.
                    var procesado = await _pedidoCommandService.ConfirmarPagoAsync(infoPago, HttpContext.RequestAborted);
                    _logger.LogInformation("Webhook procesado para Pago {Id} con estado {Estado}. Resultado efectivo: {Procesado}.", finalId, infoPago.Estado, procesado);
                }
            }

            // SIEMPRE responder 200 a MP para evitar reintentos infinitos
            return Ok();
        }

        // ============================================================================
        // ENDPOINT DE DIAGNÓSTICO - SOLO PARA DESARROLLO
        // Usar para ver headers exactos que envía MercadoPago sin validación de firma
        // ELIMINAR o deshabilitar en producción
        // ============================================================================
#if DEBUG
        [HttpPost("debug")]
        [HttpGet("debug")]
        [AllowAnonymous]
        [Obsolete("Endpoint de diagnóstico - solo para desarrollo. Eliminar en producción.")]
        public IActionResult DebugWebhook([FromQuery] string? topic, [FromQuery(Name = "id")] string? resourceId, [FromBody] object? body)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            _logger.LogWarning("=== ENDPOINT DE DIAGNÓSTICO - Webhook recibido ===");
            _logger.LogWarning("Method: {Method}, Path: {Path}", Request.Method, Request.Path);
            _logger.LogWarning("Query - topic: {Topic}, id: {ResourceId}", topic, resourceId);
            _logger.LogWarning("All Headers: {@Headers}", Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));
            _logger.LogWarning("Body: {Body}", body != null ? JsonSerializer.Serialize(body, new JsonSerializerOptions { WriteIndented = true }) : "null");

            var response = new
            {
                message = "Endpoint de diagnóstico - Solo desarrollo",
                timestamp = DateTime.Now,
                receivedData = new
                {
                    method = Request.Method,
                    path = Request.Path,
                    query = new { topic, resourceId },
                    headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    hasBody = body != null
                }
            };

            return Ok(response);
        }
#endif
    }
}
