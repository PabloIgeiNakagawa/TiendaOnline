using TiendaOnline.Features.Shared;
using Xunit;

namespace TiendaOnline.Tests.Features
{
    /// <summary>
    /// El texto "hace X" compartido en pantalla (reparto y Mis Pedidos del cliente):
    /// instantes / minutos / horas, y de 48 h para arriba en días.
    /// </summary>
    public class TiempoTranscurridoTests
    {
        [Theory]
        [InlineData(0.3, "hace instantes")]
        [InlineData(45, "hace 45 min")]
        [InlineData(90, "hace 1 h")]
        [InlineData(1500, "hace 25 h")]
        [InlineData(2940, "hace 2 días")]
        [InlineData(408840, "hace 283 días")]
        public void Formatear_ElDeltaQuePide_ElTextoEnPantalla(double minutos, string esperado)
        {
            Assert.Equal(esperado, TiempoTranscurrido.Formatear(TimeSpan.FromMinutes(minutos)));
        }
    }
}
