using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TiendaOnline.Features.Shared.Controllers
{
    /// <summary>
    /// Base de los controllers de la cola de entregas del repartidor: declara
    /// el layout repartidor para toda vista que sirvan (Features/_ViewStart lo
    /// consume de ViewData[ViewDataKeys.Layout]). La autorización sigue
    /// viviendo en cada controller — acá solo se viste la página.
    /// </summary>
    public abstract class RepartidorControllerBase : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // En OnActionExecuting y no en el constructor: MVC repone el ViewData
            // del controller durante la activación y pisaría lo seteado antes.
            ViewData[ViewDataKeys.Layout] = LayoutKeys.Repartidor;
            base.OnActionExecuting(context);
        }
    }
}
