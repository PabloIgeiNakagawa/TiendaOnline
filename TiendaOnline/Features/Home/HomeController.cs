using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Features.Shared.ViewModels;

namespace TiendaOnline.Features.Home
{
    public class HomeController : Controller
    {
        private readonly IAdminOverviewService _adminOverviewService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IAdminOverviewService adminOverviewService)
        {
            _logger = logger;
            _adminOverviewService = adminOverviewService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("Privacidad")] 
        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Admin")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> IndexAdmin()
        {
            ViewData["Title"] = "Inicio";

            var datosHome = await _adminOverviewService.ObtenerResumenHomeAsync();

            var viewModel = new HomeAdminViewModel
            {
                EstaDbOnline = datosHome.DbOnline,
                VersionApp = datosHome.AppVersion,
                Entorno = datosHome.Environment,

                UltimosCambios = datosHome.UltimosCambios.Select(a => new AuditoriaItemViewModel
                {
                    Usuario = a.UsuarioNombre,
                    Accion = a.Accion,
                    Fecha = a.Fecha
                }).ToList(),

                PedidosEstancados = datosHome.PedidosEstancados.Select(p => new PedidoEstancadoViewModel
                {
                    PedidoId = p.PedidoId,
                    Cliente = p.ClienteNombre,
                    Fecha = p.Fecha,
                    HorasTranscurridas = p.HorasTranscurridas
                }).ToList()
            };

            return View(viewModel);
        }

        [AllowAnonymous]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
