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

        [Route("admin")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> IndexAdmin()
        {
            var datosHome = await _adminOverviewService.ObtenerResumenHomeAsync();

            var viewModel = new HomeAdminViewModel
            {
                EstaDbOnline = datosHome.DbOnline,
                VersionApp = datosHome.AppVersion,
                Entorno = datosHome.Environment,

                ResumenDiario = new ResumenDiarioViewModel
                {
                    VentasHoy = datosHome.ResumenDiario.VentasHoy,
                    PorcentajeVentas = datosHome.ResumenDiario.PorcentajeVentas,
                    PedidosHoy = datosHome.ResumenDiario.PedidosHoy,
                    PorcentajePedidos = datosHome.ResumenDiario.PorcentajePedidos,
                    EnviadosHoy = datosHome.ResumenDiario.EnviadosHoy,
                    PorcentajeEnviados = datosHome.ResumenDiario.PorcentajeEnviados,
                    StockBajo = datosHome.ResumenDiario.StockBajo
                },

                UltimosCambios = datosHome.UltimosCambios.Select(a => new AuditoriaItemViewModel
                {
                    Usuario = a.UsuarioNombre,
                    Accion = a.Accion,
                    Fecha = a.Fecha
                }).ToList(),

                PaginaActual = 1,
                TotalPaginas = 1,
                TotalRegistros = 0,

                UltimosMovimientosStock = datosHome.UltimosMovimientosStock.Select(m => new MovimientoStockItemViewModel
                {
                    ProductoNombre = m.ProductoNombre,
                    Cantidad = m.Cantidad,
                    TipoMovimientoId = m.TipoMovimientoId,
                    Fecha = m.Fecha,
                    Observaciones = m.Observaciones
                }).ToList(),

                ProductosBajoStock = datosHome.ProductosBajoStock.Select(p => new ProductoBajoStockItemViewModel
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    ImagenUrl = p.ImagenUrl,
                    Categoria = p.Categoria,
                    Stock = p.Stock
                }).ToList(),

                PedidosRecientes = datosHome.PedidosRecientes.Select(p => new PedidoRecienteItemViewModel
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    Cliente = p.Cliente,
                    Total = p.Total,
                    EstadoPedido = p.EstadoPedido,
                    EstadoPedidoId = p.EstadoPedidoId
                }).ToList()
            };

            return View(viewModel);
        }

        [Route("admin/pedidos-estancados")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ObtenerPedidosEstancados(int pagina = 1)
        {
            var resultado = await _adminOverviewService.ObtenerPedidosEstancadosPaginadoAsync(pagina);

            var pedidos = resultado.Pedidos.Select(p => new PedidoEstancadoViewModel
            {
                PedidoId = p.PedidoId,
                Cliente = p.ClienteNombre,
                Fecha = p.Fecha,
                HorasTranscurridas = p.HorasTranscurridas
            }).ToList();

            return Json(new
            {
                pedidos,
                paginaActual = resultado.PaginaActual,
                totalPaginas = resultado.TotalPaginas,
                totalRegistros = resultado.TotalRegistros
            });
        }

        [AllowAnonymous]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("Home/Error429")]
        public IActionResult Error429()
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
