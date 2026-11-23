using System.Text.RegularExpressions;
using Xunit;

namespace TiendaOnline.Tests.Persistence
{
    /// <summary>
    /// Los roles de [Authorize] se separan por coma SIN espacio. ASP.NET Core hoy
    /// recorta cada entrada, así que "Administrador, Repartidor" funciona — pero el
    /// formato canónico no depende de ese detalle del framework. Un barrido del
    /// source del Web falla si aparece un espacio después de la coma.
    /// </summary>
    public class ReglaDeRolesTests
    {
        [Fact]
        public void AtributosDeRoles_NoLlevanEspacioDespuesDeLaComa()
        {
            var dirWeb = BuscarProyectoWeb();
            var conEspacio = new List<string>();

            foreach (var archivo in Directory.EnumerateFiles(dirWeb, "*.cs", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            }))
            {
                if (archivo.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || archivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    continue;

                var contenido = File.ReadAllText(archivo);
                if (Regex.IsMatch(contenido, @"Roles\s*=\s*""[^""]*,\s"))
                    conEspacio.Add(Path.GetRelativePath(dirWeb, archivo));
            }

            Assert.True(conEspacio.Count == 0,
                "Roles con espacio después de la coma (usar \"Administrador,Repartidor\"): "
                + string.Join(", ", conEspacio));
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
