using Microsoft.AspNetCore.Mvc.Razor;

namespace TiendaOnline.Extensions
{
    public class FeatureViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context) { }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            // {0} = Acción (Vista), {1} = Controlador, {2} = Area
            return new[]
            {
                // Para el Admin (Áreas) -> Features/Admin/Producto/Catalogo.cshtml
                "/Features/{2}/{1}/{0}.cshtml", 
                
                // Para la Tienda (Sin Área) -> Features/Tienda/Catalogo/Index.cshtml
                // Nota: Aquí asumo que en Tienda agrupas por funcionalidad como "Catalogo" o "Carrito"
                "/Features/Tienda/{1}/{0}.cshtml",
                
                // Para archivos compartidos -> Features/Shared/_Layout.cshtml
                "/Features/Shared/{0}.cshtml"
            };
        }
    }
}