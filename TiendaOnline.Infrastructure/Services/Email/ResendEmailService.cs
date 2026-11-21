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
                // El asunto alcanza para operar la entrega; el destino es dato
                // personal del cliente y queda fuera de los logs.
                _logger.LogInformation("Email enviado con asunto: {Asunto}", asunto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email con asunto: {Asunto}", asunto);
                throw;
            }
        }

        public async Task EnviarConfirmacionPedidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
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
<a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver mi pedido</a>";

            var titulo = $"&iexcl;Gracias por tu compra, {nombre}!";
            var cuerpoHtml = EmailTemplates.ArmarHtml(titulo, "#0d6efd", "#0d6efd", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Confirmación de pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPagoExitosoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Te confirmamos que hemos recibido el pago de tu pedido. &iexcl;Muchas gracias!</p>
<div class=""destacado"">
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
<a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver detalles de mi pedido</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("&iexcl;Pago Recibido Correctamente!", "#198754", "#198754", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Pago recibido - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPagoRechazadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Te informamos que el pago de tu pedido no pudo ser aprobado o fue cancelado antes de completarse.</p>
<div class=""destacado"">
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
<a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Revisar mi pedido</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("No pudimos confirmar tu pago", "#dc3545", "#0d6efd", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Pago no aprobado - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPagoReembolsadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Te confirmamos que el pago asociado a tu pedido fue reembolsado correctamente.</p>
<div class=""destacado"">
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
<a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver detalles de mi pedido</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("Procesamos el reembolso de tu pago", "#fd7e14", "#fd7e14", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Reembolso procesado - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        public async Task EnviarEmailPedidoVencidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Tu pedido no registr&oacute; un pago aprobado dentro de las 24 horas y fue cancelado autom&aacute;ticamente.</p>
<div class=""destacado"">
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
<a href=""{siteUrl}/Pedidos/Detalles/{pedidoId}"" class=""btn"">Ver mi pedido</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("Tu pedido venci&oacute; por falta de pago", "#6c757d", "#0d6efd", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Pedido vencido - #{pedidoId:D6}", cuerpoHtml);
        }

        // La entrega la marca el reparto o el admin: el cliente recibe la confirmación
        // de que su pedido llegó (cierra el loop de "¿dónde está mi pedido?").
        public async Task EnviarEmailPedidoEntregadoAsync(string destinoEmail, string nombreUsuario, int pedidoId)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Tu pedido ya est&aacute; en tus manos. Esperamos que lo disfrutes.</p>
<div class=""destacado"">
    <h2>&#9989; Entregado</h2>
    <p>Si algo no sali&oacute; como esperabas, escribinos y lo resolvemos.</p>
</div>
<div class=""order-box"">
    <div class=""row"">
        <span>N&uacute;mero de pedido:</span>
        <strong>#{pedidoId:D6}</strong>
    </div>
</div>
<a href=""{siteUrl}/Pedidos/MisPedidos"" class=""btn"">Ver mis pedidos</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("¡Tu pedido fue entregado!", "#198754", "#198754", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Pedido entregado - #{pedidoId:D6}", cuerpoHtml);
        }

        // Aviso interno: le llega al dueño cuando un cliente sube comprobante de transferencia.
        public async Task EnviarEmailComprobanteRecibidoAsync(string destinoEmail, int pedidoId)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');

            var contenido = $@"
<p>Un cliente subi&oacute; el comprobante de su pedido para pago por transferencia bancaria.</p>
<p><strong>Pedido:</strong> #{pedidoId:D6}</p>
<p>Verific&aacute; la transferencia y marc&aacute; el pago como aprobado (o rechazalo si no corresponde) desde el panel de pedidos.</p>
<a href=""{siteUrl}/admin/pedidos"" class=""btn"">Ir al panel de pedidos</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("Nuevo comprobante de transferencia", "#198754", "#0d6efd", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, $"Comprobante recibido - Pedido #{pedidoId:D6}", cuerpoHtml);
        }

        // El link apunta al form de nueva contraseña; el token viaja en la query
        // (firma de DataProtection, vence en 30 min y muere al usarse).
        public async Task EnviarEmailResetPasswordAsync(string destinoEmail, string nombreUsuario, string token)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var enlace = $"{siteUrl}/Accounts/ResetPassword?token={Uri.EscapeDataString(token)}";
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Nos pediste restablecer tu contrase&ntilde;a. Hac&eacute; click en el bot&oacute;n para crear una nueva:</p>
<a href=""{enlace}"" class=""btn"">Crear nueva contrase&ntilde;a</a>
<div class=""note"">
    El enlace vence a los <strong>30 minutos</strong> y se usa una sola vez. Si no fuiste vos quien lo pidi&oacute;, ignor&aacute; este email: tu contrase&ntilde;a actual sigue vigente.
</div>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("Recuperar tu contrase&ntilde;a", "#0d6efd", "#0d6efd", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, "Recuperar tu contraseña", cuerpoHtml);
        }

        // Aviso de seguridad: alguien aplicó un reset (o cambió la contraseña).
        public async Task EnviarEmailPasswordCambiadaAsync(string destinoEmail, string nombreUsuario)
        {
            var nombreSitio = _appSettings.GetValue(AppSettingsKeys.NombreDelSitio) ?? "Tienda";
            var siteUrl = _globalSettings.SiteUrl.TrimEnd('/');
            var nombre = EmailTemplates.CodificarHtml(nombreUsuario);

            var contenido = $@"
<p>Hola {nombre},</p>
<p>Te confirmamos que la contrase&ntilde;a de tu cuenta fue actualizada correctamente.</p>
<div class=""destacado"">
    <h2>&#128274; Contrase&ntilde;a actualizada</h2>
    <p>Inici&aacute; sesi&oacute;n con la nueva contrase&ntilde;a desde ac&aacute;.</p>
</div>
<p>Si <strong>no fuiste vos</strong>, contactate con soporte urgentemente: alguien m&aacute;s podr&iacute;a tener acceso a tu cuenta.</p>
<a href=""{siteUrl}/Accounts/Login"" class=""btn"">Iniciar sesi&oacute;n</a>";

            var cuerpoHtml = EmailTemplates.ArmarHtml("Tu contrase&ntilde;a fue cambiada", "#dc3545", "#0d6efd", nombreSitio, contenido);

            await EnviarEmailAsync(destinoEmail, "Tu contraseña fue cambiada", cuerpoHtml);
        }
    }
}
