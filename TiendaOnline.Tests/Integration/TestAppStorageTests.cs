using Hangfire;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// El AddHangfire del host pisa el JobStorage.Current global con su storage:
    /// si el fixture no lo repone al desbotear, ese storage queda dispuesto como
    /// Current y los tests unitarios que encolan después de los de integración
    /// revientan según el orden de corrida (el flaky de CI del 08/11).
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class TestAppStorageTests
    {
        [Fact]
        public void Dispose_AlDesbotearLaApp_ReponeElStorageGlobal()
        {
            // Arrange: el storage vigente antes de armarse el host (el de TestDatabase).
            var storageVigente = JobStorage.Current;
            var app = new TestApp();

            // Act: arrancar el host (pisa el global con su storage) y desbotearlo.
            using (app.CreateClient())
            {
            }
            app.Dispose();

            // Assert: el global volvió al storage de entrada, no al del host muerto.
            Assert.Same(storageVigente, JobStorage.Current);
        }
    }
}
