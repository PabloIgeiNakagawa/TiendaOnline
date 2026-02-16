using Microsoft.AspNetCore.Mvc;

namespace TiendaOnline.Controllers
{
    public class SoporteController : Controller
    {
        public SoporteController() { }

        public IActionResult Contacto()
        {
            ViewData["Title"] = "Contacto";
            return View();
        }

        public IActionResult FAQ()
        {
            ViewData["Title"] = "Preguntas frecuentes";
            return View();
        }

    }
}
