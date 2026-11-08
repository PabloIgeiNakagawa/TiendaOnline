using TiendaOnline.Infrastructure.ExternalServices;
using Xunit;

namespace TiendaOnline.Tests.Services.Payment
{
    /// <summary>
    /// Piso y techo de la transformación de subida: las dimensiones que pide un
    /// caller nunca generan variantes fuera de rango. Cada imagen nueva consume
    /// capacidad del plan de Cloudinary y una dimensión gigante delata el costo.
    /// </summary>
    public class CloudinaryServiceDimensionesTests
    {
        [Theory]
        [InlineData(16)]
        [InlineData(800)]
        [InlineData(2048)]
        public void LimitarDimension_DentroDelRango_SinCambios(int dimension)
        {
            Assert.Equal(dimension, CloudinaryService.LimitarDimension(dimension));
        }

        [Theory]
        [InlineData(0, 16)]
        [InlineData(15, 16)]
        [InlineData(5000, 2048)]
        [InlineData(int.MaxValue, 2048)]
        public void LimitarDimension_FueraDelRango_VuelveAlPisoOTecho(int pedida, int esperada)
        {
            Assert.Equal(esperada, CloudinaryService.LimitarDimension(pedida));
        }
    }
}
