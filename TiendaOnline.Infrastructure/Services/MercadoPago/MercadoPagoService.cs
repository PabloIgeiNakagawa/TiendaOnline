using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Payment;

public class MercadoPagoService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly IAppSettingsService _appSettingsService;

    public MercadoPagoService(IConfiguration configuration, IAppSettingsService appSettingsService)
    {
        _configuration = configuration;
        MercadoPagoConfig.AccessToken = _configuration["MercadoPago:AccessToken"];
        _appSettingsService = appSettingsService;
    }

    public async Task<string> GenerarPreferenciaPagoAsync(PedidoPagoDto pedidoDto)
    {
        var client = new PreferenceClient();
        var notificationUrl = _configuration["MercadoPago:NotificationUrl"];
        var successUrl = _configuration["MercadoPago:BackUrls:Success"];
        var failureUrl = _configuration["MercadoPago:BackUrls:Failure"];

        // Validación defensiva: si las URLs son nulas, lanzá una excepción clara
        if (string.IsNullOrEmpty(successUrl))
            throw new Exception("La URL de retorno (Success) no está configurada en appsettings.json");

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
                Estado = payment.Status,
                TransaccionId = payment.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error al consultar la API de MP para el pago {PaymentId}", paymentId);
            return null;
        }
    }

    public bool ValidarFirma(string signatureHeader, string requestId, string resourceId, string topic)
    {
        try
        {
            var secret = _configuration["MercadoPago:WebhookSecret"];
            if (string.IsNullOrEmpty(secret)) return false;

            // La firma de MP se compone de varios elementos:
            // x-signature tiene un formato: v1=hash,ts=timestamp
            var parts = signatureHeader.Split(',');
            var ts = parts.FirstOrDefault(p => p.StartsWith("ts="))?.Replace("ts=", "");
            var v1 = parts.FirstOrDefault(p => p.StartsWith("v1="))?.Replace("v1=", "");

            if (ts == null || v1 == null) return false;

            // Armamos el manifiesto según la documentación de MP
            // template: id:[resourceId];request-id:[requestId];ts:[ts];
            var manifest = $"id:{resourceId};request-id:{requestId};ts:{ts};";

            // Generamos el hash HMAC-SHA256
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Comparamos nuestra generación con la que mandó MP
                return hashString == v1;
            }
        }
        catch
        {
            return false;
        }
    }
}