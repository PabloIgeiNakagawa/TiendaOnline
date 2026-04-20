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
            <p>Tu pedido fue registrado correctamente y qued&oacute; pendiente de acreditaci&oacute;n de pago.</p>
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
            <p>Cuando el pago se confirme te avisaremos por email y tu pedido pasar&aacute; a preparaci&oacute;n.</p>
            <a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver mi pedido</a>
        </div>
        <div class=""footer"">
            <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
        </div>
        </div>
        </body>
        </html>";

            await EnviarEmailAsync(destinoEmail, $"Confirmaci&oacute;n de pedido #{pedidoId:D6}", cuerpoHtml);        
        }

        public async Task EnviarEmailPagoExitosoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
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
        .header {{ background: #198754; color: white; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 24px; }}
        .content p {{ color: #333; line-height: 1.6; }}
        .payment-box {{ border: 2px solid #198754; border-radius: 8px; padding: 20px; text-align: center; margin: 16px 0; background-color: #f8fffb; }}
        .payment-box h2 {{ color: #198754; margin-top: 0; }}
        .order-box {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 16px 0; }}
        .order-box .row {{ display: flex; justify-content: space-between; padding: 6px 0; }}
        .btn {{ display: inline-block; background: #198754; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 16px; }}
        .footer {{ background: #f8f9fa; padding: 16px; text-align: center; color: #6c757d; font-size: 13px; }}
        </style>
        </head>
        <body>
        <div class=""container"">
        <div class=""header"">
            <h1>&iexcl;Pago Recibido Correctamente!</h1>
        </div>
        <div class=""content"">
            <p>Hola {nombreUsuario},</p>
            <p>Te confirmamos que hemos recibido el pago de tu pedido. &iexcl;Muchas gracias!</p>
            
            <div class=""payment-box"">
                <h2>Pago Confirmado</h2>
                <p>Tu pedido #{pedidoId:D6} ha pasado a estado <strong>En Preparaci&oacute;n</strong>.</p>
            </div>

            <div class=""order-box"">
                <div class=""row"">
                    <span>N&uacute;mero de pedido:</span>
                    <strong>#{pedidoId:D6}</strong>
                </div>
                <div class=""row"">
                    <span>Monto pagado:</span>
                    <strong>${total:N2}</strong>
                </div>
            </div>

            <p>En cuanto el paquete est&eacute; en camino, te enviaremos otro email con los detalles del env&iacute;o.</p>
            <a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver detalles de mi pedido</a>
        </div>
        <div class=""footer"">
            <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
        </div>
        </div>
        </body>
        </html>";

            await EnviarEmailAsync(destinoEmail, $"Pago recibido - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPagoRechazadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
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
        .header {{ background: #dc3545; color: white; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 24px; }}
        .content p {{ color: #333; line-height: 1.6; }}
        .payment-box {{ border: 2px solid #dc3545; border-radius: 8px; padding: 20px; text-align: center; margin: 16px 0; background-color: #fff8f8; }}
        .payment-box h2 {{ color: #dc3545; margin-top: 0; }}
        .order-box {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 16px 0; }}
        .order-box .row {{ display: flex; justify-content: space-between; padding: 6px 0; }}
        .btn {{ display: inline-block; background: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 16px; }}
        .footer {{ background: #f8f9fa; padding: 16px; text-align: center; color: #6c757d; font-size: 13px; }}
        </style>
        </head>
        <body>
        <div class=""container"">
        <div class=""header"">
            <h1>No pudimos confirmar tu pago</h1>
        </div>
        <div class=""content"">
            <p>Hola {nombreUsuario},</p>
            <p>Te informamos que el pago de tu pedido no pudo ser aprobado o fue cancelado antes de completarse.</p>

            <div class=""payment-box"">
                <h2>Pago no acreditado</h2>
                <p>Tu pedido #{pedidoId:D6} contin&uacute;a sin pago confirmado.</p>
            </div>

            <div class=""order-box"">
                <div class=""row"">
                    <span>N&uacute;mero de pedido:</span>
                    <strong>#{pedidoId:D6}</strong>
                </div>
                <div class=""row"">
                    <span>Monto intentado:</span>
                    <strong>${total:N2}</strong>
                </div>
            </div>

            <p>Si quer&eacute;s, podes revisar tu pedido e intentar nuevamente con otro medio de pago.</p>
            <a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Revisar mi pedido</a>
        </div>
        <div class=""footer"">
            <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
        </div>
        </div>
        </body>
        </html>";

            await EnviarEmailAsync(destinoEmail, $"Pago no aprobado - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPagoReembolsadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
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
        .header {{ background: #fd7e14; color: white; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 24px; }}
        .content p {{ color: #333; line-height: 1.6; }}
        .payment-box {{ border: 2px solid #fd7e14; border-radius: 8px; padding: 20px; text-align: center; margin: 16px 0; background-color: #fff9f2; }}
        .payment-box h2 {{ color: #fd7e14; margin-top: 0; }}
        .order-box {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 16px 0; }}
        .order-box .row {{ display: flex; justify-content: space-between; padding: 6px 0; }}
        .btn {{ display: inline-block; background: #fd7e14; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 16px; }}
        .footer {{ background: #f8f9fa; padding: 16px; text-align: center; color: #6c757d; font-size: 13px; }}
        </style>
        </head>
        <body>
        <div class=""container"">
        <div class=""header"">
            <h1>Procesamos el reembolso de tu pago</h1>
        </div>
        <div class=""content"">
            <p>Hola {nombreUsuario},</p>
            <p>Te confirmamos que el pago asociado a tu pedido fue reembolsado correctamente.</p>

            <div class=""payment-box"">
                <h2>Reembolso emitido</h2>
                <p>El importe correspondiente al pedido #{pedidoId:D6} fue marcado como <strong>reembolsado</strong>.</p>
            </div>

            <div class=""order-box"">
                <div class=""row"">
                    <span>N&uacute;mero de pedido:</span>
                    <strong>#{pedidoId:D6}</strong>
                </div>
                <div class=""row"">
                    <span>Monto reembolsado:</span>
                    <strong>${total:N2}</strong>
                </div>
            </div>

            <p>Si necesit&aacute;s revisar el detalle del pedido, pod&eacute;s hacerlo desde tu cuenta.</p>
            <a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver detalles de mi pedido</a>
        </div>
        <div class=""footer"">
            <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
        </div>
        </div>
        </body>
        </html>";

            await EnviarEmailAsync(destinoEmail, $"Reembolso procesado - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPedidoVencidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
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
        .header {{ background: #6c757d; color: white; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 24px; }}
        .content p {{ color: #333; line-height: 1.6; }}
        .payment-box {{ border: 2px solid #6c757d; border-radius: 8px; padding: 20px; text-align: center; margin: 16px 0; background-color: #f8f9fa; }}
        .payment-box h2 {{ color: #495057; margin-top: 0; }}
        .order-box {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 16px 0; }}
        .order-box .row {{ display: flex; justify-content: space-between; padding: 6px 0; }}
        .btn {{ display: inline-block; background: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 16px; }}
        .footer {{ background: #f8f9fa; padding: 16px; text-align: center; color: #6c757d; font-size: 13px; }}
        </style>
        </head>
        <body>
        <div class=""container"">
        <div class=""header"">
            <h1>Tu pedido venci&oacute; por falta de pago</h1>
        </div>
        <div class=""content"">
            <p>Hola {nombreUsuario},</p>
            <p>Tu pedido no registr&oacute; un pago aprobado dentro de las 24 horas y fue cancelado autom&aacute;ticamente.</p>

            <div class=""payment-box"">
                <h2>Pedido cancelado por vencimiento</h2>
                <p>Si todav&iacute;a quer&eacute;s comprar estos productos, deber&aacute;s iniciar un nuevo pedido.</p>
            </div>

            <div class=""order-box"">
                <div class=""row"">
                    <span>N&uacute;mero de pedido:</span>
                    <strong>#{pedidoId:D6}</strong>
                </div>
                <div class=""row"">
                    <span>Total del pedido:</span>
                    <strong>${total:N2}</strong>
                </div>
            </div>

            <a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver mi pedido</a>
        </div>
        <div class=""footer"">
            <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
        </div>
        </div>
        </body>
        </html>";

            await EnviarEmailAsync(destinoEmail, $"Pedido vencido - #{pedidoId:D6}", cuerpoHtml);
        }
    }
}
