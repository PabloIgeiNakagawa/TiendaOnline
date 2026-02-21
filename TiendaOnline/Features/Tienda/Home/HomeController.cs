using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Features.Tienda.Home
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Inicio";
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Política de Privacidad";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
