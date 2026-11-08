using Microsoft.Playwright;

namespace TiendaOnline.E2E
{
    /// <summary>
    /// Cuando un paso de un smoke test explota, deja una captura de la pantalla en
    /// %TEMP%\opencode antes de re-lanzar la excepción: diagnosticar un smoke rojo
    /// sin reproducirlo a mano en el browser.
    /// </summary>
    internal static class CapturasDeFalla
    {
        public static async Task SiFallaAsync(IPage page, string nombreDelPaso, Func<Task> accion)
        {
            try
            {
                await accion();
            }
            catch
            {
                var carpeta = Path.Combine(Path.GetTempPath(), "opencode");
                Directory.CreateDirectory(carpeta);
                var timestamp = DateTime.Now.ToString("HHmmss-fff");
                var archivo = Path.Combine(carpeta, $"e2e-{nombreDelPaso}-{timestamp}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = archivo, FullPage = true });

                // Contexto del estado en el momento del fallo: la URL real de la
                // página y los valores de los inputs cuyo contenido puede explicar
                // el paso roto (email/password de los formularios de auth).
                var contexto = $"Url={page.Url}\n";
                foreach (var id in new[] { "#email", "#password", "#campoEmail" })
                {
                    try
                    {
                        contexto += $"{id}={await page.InputValueAsync(id)}\n";
                    }
                    catch
                    {
                        // El input no existe en la página actual: no molesta al diagnóstico.
                    }
                }
                await File.WriteAllTextAsync(Path.Combine(carpeta, $"e2e-{nombreDelPaso}-{timestamp}.txt"), contexto);
                throw;
            }
        }
    }
}
