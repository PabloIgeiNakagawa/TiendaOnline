using System.Text.RegularExpressions;
using Xunit;

namespace TiendaOnline.Tests.Persistence
{
    /// <summary>
    /// Los mensajes flash se escriben SOLO por la API tipada (SetMensajeExito/Error,
    /// extensiones de TempDataExtensions). Un TempData["..."] con literal compila igual
    /// y falla en silencio: el parcial lector nunca renderiza ese key (ya pasó:
    /// un "Error" huérfano perdía el aviso de borrar una categoría con productos).
    /// Barre el source del proyecto Web y falla si aparece la literal fuera de la API.
    /// </summary>
    public class ReglaDeMensajesFlashTests
    {
        [Fact]
        public void NingunArchivoUsaKeysDeTempDataLiterales()
        {
            // Arrange: el csproj del Web es el ancla para caminar hasta la raíz del repo
            var dirWeb = BuscarProyectoWeb();
            var consumidoresLiterales = new List<string>();

            // Act: barre .cs y .cshtml (todos los lectores/escritores viven acá)
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
                if (Regex.IsMatch(contenido, @"TempData\["""))
                    consumidoresLiterales.Add(Path.GetRelativePath(dirWeb, archivo));
            }

            // Assert
            Assert.True(consumidoresLiterales.Count == 0,
                "Keys de TempData escritos con literal (usar TempDataExtensions): "
                + string.Join(", ", consumidoresLiterales));
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
