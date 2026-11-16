using System.Text.Encodings.Web;

namespace TiendaOnline.Infrastructure.Services.Email
{
    /// <summary>
    /// El esqueleto HTML que comparten los avisos de ResendEmailService: doctype,
    /// estilos, header con el título, contenido y footer con el nombre del sitio.
    /// Vive acá solo — un cambio de look toca un archivo, no nueve copias.
    /// Todo dato controlado por el usuario que entra al contenido (el nombre del
    /// cliente, por ejemplo) debe viajar por CodificarHtml: se interpola directo en el
    /// HTML del email y un nombre con marcado inyectaría su propio mensaje
    /// (phishing potencial).
    /// </summary>
    public static class EmailTemplates
    {
        /// <summary>Codifica el dato controlado por el usuario en entidades HTML antes de interpolarlo en el email (&lt; pasa a &amp;lt;, &quot; a &amp;quot;): el texto se muestra tal cual fue escrito y el marcado no se ejecuta.</summary>
        public static string CodificarHtml(string valor) => HtmlEncoder.Default.Encode(valor);

        /// <summary>
        /// Arma el email completo. El color de acento viste header, box destacado y
        /// el borde del total; el color del botón va aparte (algunos avisos rojos
        /// mandan a una acción azul). El contenido ya llega construido por el
        /// caller, con sus datos escapados.
        /// </summary>
        public static string ArmarHtml(string titulo, string colorAcento, string colorBoton, string nombreSitio, string contenidoHtml)
        {
            return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<style>
body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }}
.container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
.header {{ background: {colorAcento}; color: white; padding: 24px; text-align: center; }}
.header h1 {{ margin: 0; font-size: 24px; }}
.content {{ padding: 24px; }}
.content p {{ color: #333; line-height: 1.6; }}
.destacado {{ border: 2px solid {colorAcento}; border-radius: 8px; padding: 20px; text-align: center; margin: 16px 0; background-color: #f8f9fa; }}
.destacado h2 {{ color: {colorAcento}; margin-top: 0; }}
.order-box {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 16px 0; }}
.order-box .row {{ display: flex; justify-content: space-between; padding: 6px 0; }}
.order-box .total {{ font-weight: bold; font-size: 18px; border-top: 2px solid {colorAcento}; padding-top: 10px; margin-top: 6px; }}
.note {{ background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 14px 16px; margin: 16px 0; font-size: 14px; color: #495057; }}
.btn {{ display: inline-block; background: {colorBoton}; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 16px; }}
.footer {{ background: #f8f9fa; padding: 16px; text-align: center; color: #6c757d; font-size: 13px; }}
</style>
</head>
<body>
<div class=""container"">
<div class=""header"">
    <h1>{titulo}</h1>
</div>
<div class=""content"">
{contenidoHtml}
</div>
<div class=""footer"">
    <p>{nombreSitio} &copy; {DateTime.Now.Year} - Este es un email autom&aacute;tico, por favor no responder.</p>
</div>
</div>
</body>
</html>";
        }
    }
}
