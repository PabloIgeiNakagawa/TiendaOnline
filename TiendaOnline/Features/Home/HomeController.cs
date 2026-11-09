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
        public IActionResult IndexAdmin()
        {
            // Caparazón: cero consultas del panel en esta acción — la página sale
            // casi de una y cada tarjeta pide su fragmento por fetch
            // (los endpoints admin/secciones/* de acá abajo; el loader y los
            // esqueletos son maquinaria de site.js con data-* de la vista).
            return View(new HomeAdminViewModel());
        }

        // Fragmentos del caparazón: los 6 fetches de una carga del panel piden
        // secciones del MISMO AdminOverviewDto, que vive guardado 30s en cache —
        // la base corre sus consultas a lo sumo una vez por ventana, no una por
        // fetch. Cada fragmento proyecta solo su tajada hacia su ViewModel.
        [Route("admin/secciones/estado-sistema")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> EstadoSistemaParcial()
        {
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(HttpContext.RequestAborted);
            return PartialView("_EstadoSistema", (datos.DbOnline, datos.AppVersion));
        }

        [Route("admin/secciones/resumen-diario")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ResumenDiarioParcial()
        {
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(HttpContext.RequestAborted);
            return PartialView("_ResumenDiario", new ResumenDiarioViewModel
            {
                VentasHoy = datos.ResumenDiario.VentasHoy,
                PorcentajeVentas = datos.ResumenDiario.PorcentajeVentas,
                PedidosHoy = datos.ResumenDiario.PedidosHoy,
                PorcentajePedidos = datos.ResumenDiario.PorcentajePedidos,
                EnviadosHoy = datos.ResumenDiario.EnviadosHoy,
                PorcentajeEnviados = datos.ResumenDiario.PorcentajeEnviados,
                StockBajo = datos.ResumenDiario.StockBajo
            });
        }

        [Route("admin/secciones/ultimos-cambios")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> UltimosCambiosParcial()
        {
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(HttpContext.RequestAborted);
            return PartialView("_UltimosCambios", datos.UltimosCambios.Select(a => new AuditoriaItemViewModel
            {
                Usuario = a.UsuarioNombre,
                Accion = a.Accion,
                Fecha = a.Fecha
            }).ToList());
        }

        [Route("admin/secciones/movimientos-stock")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> MovimientosStockParcial()
        {
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(HttpContext.RequestAborted);
            return PartialView("_MovimientosStock", datos.UltimosMovimientosStock.Select(m => new MovimientoStockItemViewModel
            {
                ProductoNombre = m.ProductoNombre,
                Cantidad = m.Cantidad,
                TipoMovimientoId = m.TipoMovimientoId,
                Fecha = m.Fecha,
                Observaciones = m.Observaciones
            }).ToList());
        }

        [Route("admin/secciones/productos-bajo-stock")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ProductosBajoStockParcial()
        {
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(HttpContext.RequestAborted);
            return PartialView("_ProductosBajoStock", datos.ProductosBajoStock.Select(p => new ProductoBajoStockItemViewModel
            {
                ProductoId = p.ProductoId,
                Nombre = p.Nombre,
                ImagenUrl = p.ImagenUrl,
                Categoria = p.Categoria,
                Stock = p.Stock
            }).ToList());
        }

        [Route("admin/secciones/pedidos-recientes")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> PedidosRecientesParcial()
        {
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(HttpContext.RequestAborted);
            return PartialView("_PedidosRecientes", datos.PedidosRecientes.Select(p => new PedidoRecienteItemViewModel
            {
                PedidoId = p.PedidoId,
                FechaPedido = p.FechaPedido,
                Cliente = p.Cliente,
                Total = p.Total,
                EstadoPedido = p.EstadoPedido,
                EstadoPedidoId = p.EstadoPedidoId
            }).ToList());
        }

        [Route("admin/pedidos-estancados")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> ObtenerPedidosEstancados(int pagina = 1)
        {
            var resultado = await _adminOverviewService.ObtenerPedidosEstancadosPaginadoAsync(pagina, ct: HttpContext.RequestAborted);

            // Caparazón + parcial: el fragmento es HTML rendereado por Razor (el
            // molde de las filas vive UNA vez en _AtencionRequeridaLista, no
            // copiado en JS; Razor auto-escapa el nombre del cliente, dato
            // controlado por cualquier usuario registrado).
            var modelo = new HomeAdminViewModel
            {
                PaginaActual = resultado.PaginaActual,
                TotalPaginas = resultado.TotalPaginas,
                TotalRegistros = resultado.TotalRegistros,
                PedidosEstancados = resultado.Pedidos.Select(p => new PedidoEstancadoViewModel
                {
                    PedidoId = p.PedidoId,
                    Cliente = p.ClienteNombre,
                    Fecha = p.Fecha,
                    HorasTranscurridas = p.HorasTranscurridas
                }).ToList()
            };

            return PartialView("_AtencionRequeridaLista", modelo);
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
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
