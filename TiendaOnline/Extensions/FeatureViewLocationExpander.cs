using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace TiendaOnline.Extensions
{
    public class FeatureViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context) { }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var locations = new List<string>();

            if (context.ActionContext.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                string? ns = descriptor.ControllerTypeInfo.Namespace;

                if (!string.IsNullOrEmpty(ns))
                {
                    var parts = ns.Split('.');
                    int featureIndex = Array.IndexOf(parts, "Features");

                    if (featureIndex != -1 && parts.Length > featureIndex + 1)
                    {
                        // En lugar de tomar solo la siguiente, tomamos TODO lo que sigue
                        // Ejemplo: [TiendaOnline, Features, Usuarios, Admin] 
                        // Resultado: "Usuarios/Admin"
                        var featurePath = string.Join("/", parts.Skip(featureIndex + 1));

                        // 1. Ruta basada estrictamente en el Namespace (La más segura para vos)
                        // Esto buscará en: /Features/Usuarios/Admin/CrearUsuario.cshtml
                        locations.Add($"/Features/{featurePath}/{{0}}.cshtml");

                        // 2. Ruta basada en Namespace + Nombre del Controlador
                        // Esto buscará en: /Features/Usuarios/Admin/AdminUsuarios/CrearUsuario.cshtml
                        locations.Add($"/Features/{featurePath}/{{1}}/{{0}}.cshtml");
                    }
                }
            }

            // Fallbacks
            locations.Add("/Features/{1}/{0}.cshtml");
            locations.Add("/Features/Shared/{0}.cshtml");
            locations.Add("/Views/Shared/{0}.cshtml");

            return locations;
        }
    }
}