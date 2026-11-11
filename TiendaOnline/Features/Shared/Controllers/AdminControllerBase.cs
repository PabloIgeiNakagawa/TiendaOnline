using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TiendaOnline.Features.Shared.Controllers
{
    /// <summary>
    /// Base de los controllers del panel de administración: declara el layout
    /// admin para toda vista que sirvan (Features/_ViewStart lo consume de
    /// ViewData[ViewDataKeys.Layout]). La autorización sigue viviendo en cada
    /// controller — acá solo se viste la página.
    /// </summary>
    public abstract class AdminControllerBase : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // En OnActionExecuting y no en el constructor: MVC repone el ViewData
            // del controller durante la activación y pisaría lo seteado antes.
            ViewData[ViewDataKeys.Layout] = LayoutKeys.Admin;
            base.OnActionExecuting(context);
        }
    }
}
