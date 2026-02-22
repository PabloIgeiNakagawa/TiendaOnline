using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.IServices.Admin;

namespace TiendaOnline.Features.Admin.HomeAdmin
{
    [Route("Admin")]
    [Authorize(Roles = "Administrador")]
    public class HomeAdminController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly ILogger<HomeAdminController> _logger;

        public HomeAdminController(ILogger<HomeAdminController> logger, IHomeService homeService)
        {
            _logger = logger;
            _homeService = homeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Inicio";

            var datosHome = await _homeService.ObtenerResumenHomeAsync();

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
