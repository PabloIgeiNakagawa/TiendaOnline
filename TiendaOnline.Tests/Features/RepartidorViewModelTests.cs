using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Features.Repartidor;
using Xunit;

namespace TiendaOnline.Tests.Features
{
    /// <summary>
    /// La lógica de display de la fila del reparto (lo que el rol ve en la calle):
    /// ruta con GPS, WhatsApp con solo dígitos, etiqueta de retiro y el tiempo
    /// "en camino" con su resaltado de colgados. La vista solo imprime esto.
    /// </summary>
    public class RepartidorViewModelTests
    {
        private static RepartidorFilaViewModel Fila(string? direccion, DateTime? fechaEnvio = null, string? telefono = null) => new()
        {
            PedidoId = 53,
            NombreCliente = "Cliente Prueba",
            Telefono = telefono,
            DireccionCompleta = direccion,
            FechaEnvio = fechaEnvio
        };

        [Fact]
        public void EnvioADomicilio_ArmaRutaConDestinoCodificado()
        {
            var fila = Fila("Av. Santa Fe 3253, CABA");

            Assert.False(fila.EsRetiroLocal);
            Assert.Equal("Av. Santa Fe 3253, CABA", fila.DireccionDisplay);
            Assert.Equal("https://www.google.com/maps/dir/?api=1&destination=Av.%20Santa%20Fe%203253%2C%20CABA", fila.LinkComoLlegar);
        }

        [Fact]
        public void RetiroEnLocal_NoOfreceRutaNiWhatsApp()
        {
            // El cliente viene al local: el reparto no necesita ruta ni contacto.
            var fila = Fila(null, telefono: "+54 9 11 5555-0000");

            Assert.True(fila.EsRetiroLocal);
            Assert.Equal("Retiro en local", fila.DireccionDisplay);
            Assert.Null(fila.LinkComoLlegar);
            Assert.Null(fila.LinkWhatsapp);
        }

        [Fact]
        public void WhatsApp_LimpiaElTelefonoASoloDigitos()
        {
            var fila = Fila("Av. Siempre Viva 742", telefono: "+54 9 11 2345-6789");

            Assert.Equal("https://wa.me/5491123456789", fila.LinkWhatsapp);
        }

        [Fact]
        public void WhatsApp_SinTelefonoONoDigitos_NoGeneraLink()
        {
            Assert.Null(Fila("Av. Siempre Viva 742", telefono: null).LinkWhatsapp);
            Assert.Null(Fila("Av. Siempre Viva 742", telefono: "---").LinkWhatsapp);
        }

        [Fact]
        public void HaceEnCamino_FormateaMinutosHorasYResaltaColgados()
        {
            Assert.Equal("hace instantes", Fila("x", DateTime.Now.AddSeconds(-20)).HaceEnCamino);
            Assert.Equal("hace 45 min", Fila("x", DateTime.Now.AddMinutes(-45)).HaceEnCamino);
            Assert.Equal("hace 1 h", Fila("x", DateTime.Now.AddMinutes(-90)).HaceEnCamino);
            Assert.Equal("hace 25 h", Fila("x", DateTime.Now.AddHours(-25)).HaceEnCamino);
            Assert.Equal("hace 283 días", Fila("x", DateTime.Now.AddHours(-6814)).HaceEnCamino);
            Assert.False(Fila("x", DateTime.Now.AddHours(-23)).EstaColgada);
            Assert.True(Fila("x", DateTime.Now.AddHours(-25)).EstaColgada);
        }

        [Fact]
        public void Entregado_SinFechaDeEnvio_NoMuestraTiempo()
        {
            // En la sección de entregados la fila no viaja con FechaEnvio.
            var fila = Fila("Av. Santa Fe 3253");

            Assert.Null(fila.HaceEnCamino);
            Assert.False(fila.EstaColgada);
        }
    }
}
