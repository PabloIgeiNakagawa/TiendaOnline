using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Features.Admin.HomeAdmin
{
    [Route("Admin")]
    [Authorize(Roles = "Administrador")]
    public class HomeAdminController : Controller
    {
        private readonly IAdminOverviewService _adminOverviewService;
        private readonly ILogger<HomeAdminController> _logger;

        public HomeAdminController(ILogger<HomeAdminController> logger, IAdminOverviewService adminOverviewService)
        {
            _logger = logger;
            _adminOverviewService = adminOverviewService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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

        public IActionResult Privacy()
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
