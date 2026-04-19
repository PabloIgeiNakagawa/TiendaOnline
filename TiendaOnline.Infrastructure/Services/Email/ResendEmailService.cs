using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Common.Settings;

namespace TiendaOnline.Infrastructure.Services.Email
{
    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IAppSettingsService _appSettings;
        private readonly ResendSettings _resendSettings;
        private readonly GlobalSettings _globalSettings;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(
            IResend resend, 
            IAppSettingsService appSettings, 
            IOptions<ResendSettings> resendOptions,
            IOptions<GlobalSettings> globalOptions,
            ILogger<ResendEmailService> logger)
        {
            _resend = resend;
            _appSettings = appSettings;
            _resendSettings = resendOptions.Value;
            _globalSettings = globalOptions.Value;
            _logger = logger;
        }

        public async Task EnviarEmailAsync(string destino, string asunto, string cuerpoHtml)
        {
            var fromEmail = _resendSettings.FromEmail;
            var fromName = _resendSettings.FromName;

            var mensaje = new EmailMessage
            {
                From = $"{fromName} <{fromEmail}>",
                Subject = asunto,
                HtmlBody = cuerpoHtml
            };
            mensaje.To.Add(destino);

            try
            {
                await _resend.EmailSendAsync(mensaje);
                _logger.LogInformation("Email enviado a {Destino} con asunto: {Asunto}", destino, asunto);      
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email a {Destino}", destino);
                throw;
            }
        }

        public async Task EnviarConfirmacionPedidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
        {
            var nombreSitio = _appSettings.GetValue("Diseno:NombreDelSitio") ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');

            var cuerpoHtml = $@"
        <!DOCTYPE html>
        <html lang=""es"">
        <head>
        <meta charset=""utf-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .header {{ background: #0d6efd; color: white; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 24px; }}
        .content p {{ color: #333; line-height: 1.6; }}
        .order-box {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 16px 0; }}
        .order-box .row {{ display: flex; justify-content: space-between; padding: 6px 0; }}
        .order-box .total {{ font-weight: bold; font-size: 18px; border-top: 2px solid #0d6efd; padding-top: 10px; margin-top: 6px; }}
        .btn {{ display: inline-block; background: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 16px; }}
        .footer {{ background: #f8f9fa; padding: 16px; text-align: center; color: #6c757d; font-size: 13px; }}
        </style>
        </head>
        <body>
        <div class=""container"">
        <div class=""header"">
            <h1>&iexcl;Gracias por tu compra, {nombreUsuario}!</h1>
        </div>
        <div class=""content"">
            <p>Tu pedido ha sido registrado exitosamente. Estamos preparando todo para que lo recibas lo antes posible.</p>
            <div class=""order-box"">
                <div class=""row"">
                    <span>N&uacute;mero de pedido:</span>
                    <strong>#{pedidoId:D6}</strong>
                </div>
                <div class=""row"">
                    <span>Total:</span>
                    <strong class=""total"">${total:N2}</strong>
                </div>
            </div>
            <p>Podes ver el estado de tu pedido en cualquier momento desde tu cuenta.</p>
            <a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver mi pedido</a>
        </div>
        <div class=""footer"">
            <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
        </div>
        </div>
        </body>
        </html>";

            await EnviarEmailAsync(destinoEmail, $"Confirmaci&oacute;n de pedido #{pedidoId:D6}", cuerpoHtml);        
        }    }
}
