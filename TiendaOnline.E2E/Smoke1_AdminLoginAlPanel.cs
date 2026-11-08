using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TiendaOnline.E2E
{
    /// <summary>
    /// Smoke test 1: un administrador entra por el login real y aterriza en su panel.
    /// Cubre: form con antiforgery, verificación de password hasheada, cookie de
    /// auth y el redirect por rol a /Home/IndexAdmin.
    /// </summary>
    [Collection("E2E")]
    public class Smoke1_AdminLoginAlPanel
    {
        private readonly E2EFixture _fixture;

        public Smoke1_AdminLoginAlPanel(E2EFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Login_DeUnAdministrador_AterrizaEnElPanelDeControl()
        {
            var contexto = await _fixture.CrearContextoAsync();
            var page = await contexto.NewPageAsync();

            await CapturasDeFalla.SiFallaAsync(page, "smoke1", async () =>
            {
                await _fixture.LoginAsync(page, E2EFixture.EmailAdmin);
                // El post-login del Administrador va a /admin: la acción IndexAdmin
                // vive en la ruta literal "admin" (HomeController).
                await page.WaitForURLAsync("**/admin");

                await Expect(page.GetByText("Panel de Control")).ToBeVisibleAsync();
            });

            await contexto.DisposeAsync();
        }
    }
}
