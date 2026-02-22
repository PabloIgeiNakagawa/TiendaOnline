using Microsoft.AspNetCore.Mvc;

namespace TiendaOnline.Features.Tienda.Soporte
{
    [Route("[controller]")]
    public class SoporteController : Controller
    {
        public SoporteController() { }

        [HttpGet("[action]")]
        public IActionResult Contacto()
        {
            ViewData["Title"] = "Contacto";
            return View();
        }

        [HttpGet("[action]")]
        public IActionResult FAQ()
        {
            ViewData["Title"] = "Preguntas frecuentes";
            return View();
        }

    }
}
