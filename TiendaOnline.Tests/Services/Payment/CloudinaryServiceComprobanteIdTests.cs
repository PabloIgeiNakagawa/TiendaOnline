using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TiendaOnline.Application.Common.Settings;
using TiendaOnline.Infrastructure.ExternalServices;
using Xunit;

namespace TiendaOnline.Tests.Services.Payment
{
    /// <summary>
    /// Identificador del comprobante en Cloudinary: si fuera derivado del número
    /// de pedido, la URL se adivina en serie y la evidencia de pago (CBU, titular,
    /// montos) queda pública sin autenticación. El suffix aleatorio por subida
    /// rompe ese patrón y el id sigue siendo extraíble de la URL para la baja.
    /// </summary>
    public class CloudinaryServiceComprobanteIdTests
    {
        private const string UrlVersionada = "https://res.cloudinary.com/demo/image/upload/v1715000000/";

        private static CloudinaryService Servicio()
            => new(
                Options.Create(new CloudinarySettings
                {
                    CloudName = "test-cloud",
                    ApiKey = "test-key",
                    ApiSecret = "test-secret"
                }),
                NullLogger<CloudinaryService>.Instance);

        [Fact]
        public void GenerarPublicIdDeComprobante_MantieneCarpetaYTrazaDelPedido_ConSuffixAleatorio()
        {
            var publicId = CloudinaryService.GenerarPublicIdDeComprobante("Pedido-000123.jpg");

            Assert.Matches(@"^TechStore/Comprobantes/Pedido-000123-[0-9a-f]{32}\.jpg$", publicId);
        }

        [Fact]
        public void GenerarPublicIdDeComprobante_DosSubidasDelMismoNombre_GeneranIdentificadoresDistintos()
        {
            var primero = CloudinaryService.GenerarPublicIdDeComprobante("Pedido-000123.jpg");
            var segundo = CloudinaryService.GenerarPublicIdDeComprobante("Pedido-000123.jpg");

            Assert.NotEqual(primero, segundo);
        }

        [Fact]
        public void ExtraerPublicIdDesdeUrl_DevuelveElIdSinExtensionParaLaBaja()
        {
            var publicId = CloudinaryService.GenerarPublicIdDeComprobante("Pedido-000123.pdf");
            var url = $"{UrlVersionada}{publicId}";

            // El contrato para la baja es el public id sin extensión: DestroyAsync
            // espera el id tal como está en Cloudinary, sin el formato del archivo.
            var idSinExtension = Path.ChangeExtension(publicId, null);

            Assert.Equal(idSinExtension, Servicio().ExtraerPublicIdDesdeUrl(url));
        }
    }
}
