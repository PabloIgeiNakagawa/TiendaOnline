using System.Text.RegularExpressions;
using TiendaOnline.Features.Shared;
using Xunit;

namespace TiendaOnline.Tests.Persistence
{
    /// <summary>
    /// Los keys de ViewData se escriben SOLO por la familia ViewDataKeys
    /// (mismo criterio que TempData con su API tipada). Un ViewData["Title"]
    /// con literal compila igual y si lleva un typo el layout nunca lo renderiza:
    /// la página pierde su título en silencio. Barre el source del proyecto Web
    /// y falla si aparece el literal fuera de la constant.
    /// </summary>
    public class ReglaDeTitulosDeVistaTests
    {
        [Fact]
        public void NingunArchivoUsaKeysDeViewDataLiterales()
        {
            // Arrange: el csproj del Web es el ancla para caminar hasta la raíz del repo
            var dirWeb = BuscarProyectoWeb();
            var consumidoresLiterales = new List<string>();

            // Act: barre .cs y .cshtml (todos los escritores y el layout lector viven acá)
            foreach (var archivo in Directory.EnumerateFiles(dirWeb, "*.*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            }))
            {
                var extension = Path.GetExtension(archivo);
                if (extension is not (".cs" or ".cshtml")) continue;
                if (archivo.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || archivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    continue;

                var contenido = File.ReadAllText(archivo);
                if (Regex.IsMatch(contenido, @"ViewData\[""Title""\]"))
                    consumidoresLiterales.Add(Path.GetRelativePath(dirWeb, archivo));
            }

            // Assert
            Assert.True(consumidoresLiterales.Count == 0,
                "Keys de ViewData escritos con literal (usar ViewDataKeys): "
                + string.Join(", ", consumidoresLiterales));
        }

        [Fact]
        public void Title_MantieneElValorDelContratoInterno()
        {
            // El layout renderiza por el key del const: su valor es contrato interno
            // entre vista y layout; un cambio de valor debe pasar por acá.
            Assert.Equal("Title", ViewDataKeys.Title);
        }

        private static string BuscarProyectoWeb()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
            {
                var candidato = Path.Combine(dir.FullName, "TiendaOnline");
                if (File.Exists(Path.Combine(candidato, "TiendaOnline.csproj")))
                    return candidato;
            }

            throw new InvalidOperationException("No se resolvió el directorio del proyecto Web desde bin.");
        }
    }
}
