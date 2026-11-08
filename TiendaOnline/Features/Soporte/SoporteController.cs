using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Soporte
{
    [Route("[controller]")]
    public class SoporteController : Controller
    {
        public SoporteController() { }

        [HttpGet("[action]")]
        public IActionResult Contacto()
        {
            ViewData[ViewDataKeys.Title] = "Contacto";
            return View();
        }

        [HttpGet("[action]")]
        public IActionResult FAQ()
        {
            ViewData[ViewDataKeys.Title] = "Preguntas frecuentes";
            return View();
        }

    }
}
