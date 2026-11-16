using System.Web;
using TiendaOnline.Infrastructure.Services.Email;

namespace TiendaOnline.Tests.Services.Email
{
    public class EmailTemplatesTests
    {
        [Fact]
        public void ArmarHtml_IncluyeTituloContenidoFooterYColores()
        {
            var html = EmailTemplates.ArmarHtml(
                titulo: "&iexcl;Gracias!",
                colorAcento: "#198754",
                colorBoton: "#0d6efd",
                nombreSitio: "TechStore",
                contenidoHtml: "<p>Hola</p>");

            Assert.Contains("<h1>&iexcl;Gracias!</h1>", html);
            Assert.Contains("<p>Hola</p>", html);
            Assert.Contains("TechStore", html);
            Assert.Contains("background: #198754; color: white; padding: 24px", html);
            Assert.Contains("border: 2px solid #198754", html);
            Assert.Contains("background: #0d6efd", html);
        }

        [Fact]
        public void ArmarHtml_EscapaInterpolacionPorDobleLlave_CssSinConflictos()
        {
            // El esqueleto es un verbatim interpolado: el CSS con llaves no rompe
            // el markup (el render sería corrupto si alguna llave quedara simple).
            var html = EmailTemplates.ArmarHtml("T", "#0d6efd", "#0d6efd", "S", "<p>c</p>");

            Assert.Contains("font-family: -apple-system", html);
            Assert.Contains("</html>", html);
        }

        [Fact]
        public void CodificarHtml_NeutralizaMarcadoHtml()
        {
            var codificado = EmailTemplates.CodificarHtml("<img src=x onerror=alert(1)>");

            Assert.DoesNotContain("<img", codificado);
            Assert.Contains("&lt;img", codificado);
        }

        [Fact]
        public void CodificarHtml_ConservaTextoPlano()
        {
            Assert.Equal("Pablo", EmailTemplates.CodificarHtml("Pablo"));
        }
    }
}
