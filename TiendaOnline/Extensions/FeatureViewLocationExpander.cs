using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace TiendaOnline.Extensions
{
    public class FeatureViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context) { }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            // {0} = Acción, {1} = Controlador

            var locations = new List<string>();

            // Intentamos obtener el namespace del controlador actual
            if (context.ActionContext.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                // Ejemplo: TiendaOnline.Features.Admin.Productos
                string? ns = descriptor.ControllerTypeInfo.Namespace;

                if (!string.IsNullOrEmpty(ns))
                {
                    // Sacamos las partes del namespace para encontrar la carpeta después de ".Features."
                    // Esto nos permite detectar automáticamente "Admin", "Tienda", "Repartidor", etc.
                    var parts = ns.Split('.');
                    int featureIndex = Array.IndexOf(parts, "Features");

                    if (featureIndex != -1 && parts.Length > featureIndex + 1)
                    {
                        // Este es el "Rol" o "Contexto" (Admin, Tienda, etc.)
                        string contextFolder = parts[featureIndex + 1];

                        // Ruta dinámica: /Features/Admin/Productos/Catalogo.cshtml
                        locations.Add($"/Features/{contextFolder}/{{1}}/{{0}}.cshtml");
                    }
                }
            }

            // Fallback: Si no detecta nada o para rutas comunes
            locations.Add("/Features/{1}/{0}.cshtml");

            // Compartidos
            locations.Add("/Features/Shared/{0}.cshtml");
            locations.Add("/Views/Shared/{0}.cshtml");

            return locations;
        }
    }
}