using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Common.Settings;

public class MercadoPagoService : IPaymentService
{
    private readonly MercadoPagoSettings _settings;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(IOptions<MercadoPagoSettings> options, IAppSettingsService appSettingsService, ILogger<MercadoPagoService> logger)
    {
        _settings = options.Value;
        MercadoPagoConfig.AccessToken = _settings.AccessToken;
        _appSettingsService = appSettingsService;
        _logger = logger;
    }

    public async Task<string> GenerarPreferenciaPagoAsync(PedidoPagoDto pedidoDto)
    {
        var client = new PreferenceClient();
        var notificationUrl = _settings.NotificationUrl;
        var successUrl = _settings.BackUrls.Success;
        var failureUrl = _settings.BackUrls.Failure;

        // Validación defensiva: si las URLs son nulas, lanzá una excepción clara
        if (string.IsNullOrEmpty(successUrl))
            throw new Exception("La URL de retorno (Success) no está configurada en MercadoPagoSettings");

        var request = new PreferenceRequest
        {
            Items = pedidoDto.Items.Select(i => new PreferenceItemRequest
            {
                Title = i.Nombre,
                Description = $"Compra en {_appSettingsService.GetValue("Diseno:NombreDelSitio")} - {i.Nombre}",
                Quantity = i.Cantidad,
                UnitPrice = i.PrecioUnitario,
                CurrencyId = "ARS"
            }).ToList(),
            Payer = new PreferencePayerRequest { Email = pedidoDto.EmailUsuario },
            ExternalReference = pedidoDto.PedidoId.ToString(),
            NotificationUrl = notificationUrl,
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = successUrl,
                Failure = failureUrl
            },
            PaymentMethods = new PreferencePaymentMethodsRequest
            {
                Installments = 12, // Máximo de cuotas
            },
            AutoReturn = "approved",

            // Expira la preferencia en un tiempo determinado (opcional)
            Expires = true,
            ExpirationDateFrom = DateTime.Now,
            ExpirationDateTo = DateTime.Now.AddHours(24),
        };

        var preference = await client.CreateAsync(request);
        return preference.InitPoint;
    }

    public async Task<InfoPagoDto?> ObtenerDetallesPagoAsync(string paymentId)
    {
        try
        {
            var client = new PaymentClient();
            var payment = await client.GetAsync(long.Parse(paymentId));

            // Si falla la consulta o no tiene referencia externa, devolvemos null
            if (payment == null || string.IsNullOrEmpty(payment.ExternalReference))
                return null;

            // Mapeamos la respuesta de MP a nuestro DTO limpio
            return new InfoPagoDto
            {
                PedidoId = int.Parse(payment.ExternalReference),
                MontoPagado = payment.TransactionAmount ?? 0,
                Estado = payment.Status ?? string.Empty,
                TransaccionId = payment.Id.ToString()
            };
        }
        catch
        {
            // _logger.LogError(ex, "Error al consultar la API de MP para el pago {PaymentId}", paymentId);
            return null;
        }
    }

    public bool ValidarFirma(string signatureHeader, string requestId, string resourceId, string topic)
    {
        try
        {
            // En desarrollo, permitir bypass de la firma si está configurado explícitamente
            if (_settings.BypassSignatureValidation)
            {
                _logger.LogWarning("⚠ VALIDACIÓN DE FIRMA BYPASSEADA - Esto solo debería estar habilitado en desarrollo");
                return true;
            }

            var secret = _settings.WebhookSecret;
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("WebhookSecret no está configurado en MercadoPagoSettings");
                return false;
            }

            _logger.LogInformation("Validando firma - Header x-signature: {SignatureHeader}", signatureHeader);
            _logger.LogInformation("ResourceId: {ResourceId}, RequestId: {RequestId}, Topic: {Topic}", resourceId, requestId, topic);

            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning("Header x-signature está vacío o es nulo");
                return false;
            }

            // Parsing robusto: extraer v1 y ts sin asumir orden específico
            // Formato esperado: v1=hash,ts=timestamp O ts=timestamp,v1=hash
            var v1Match = Regex.Match(signatureHeader, @"v1=([a-f0-9]+)", RegexOptions.IgnoreCase);
            var tsMatch = Regex.Match(signatureHeader, @"ts=(\d+)");

            if (!v1Match.Success || !tsMatch.Success)
            {
                _logger.LogWarning("Formato de firma inválido. No se pudieron extraer v1 y/o ts. Header: {SignatureHeader}", signatureHeader);
                return false;
            }

            var v1 = v1Match.Groups[1].Value.ToLower();
            var ts = tsMatch.Groups[1].Value;

            _logger.LogInformation("Valores extraídos - v1: {V1}, ts: {Ts}", v1, ts);

            // Armamos el manifiesto según la documentación de MP
            // template: id:[resourceId];request-id:[requestId];ts:[ts];
            var manifest = $"id:{resourceId};request-id:{requestId};ts:{ts};";

            _logger.LogInformation("Manifiesto generado: {Manifest}", manifest);

            // Generamos el hash HMAC-SHA256
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                _logger.LogDebug("Hash calculado: {HashCalculado}, Hash recibido: {HashRecibido}", hashString, v1);

                var isValid = hashString == v1;

                if (isValid)
                {
                    _logger.LogInformation("✓ Firma de webhook valida correctamente");
                }
                else
                {
                    _logger.LogWarning("✗ Firma de webhook NO coincide. Esperado: {HashCalculado}, Recibido: {HashRecibido}", hashString, v1);
                }

                return isValid;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al validar la firma del webhook");
            return false;
        }
    }
}