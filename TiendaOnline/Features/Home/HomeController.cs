using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.ViewModels;

namespace TiendaOnline.Features.Home
{
    public class HomeController : Controller
    {
        private readonly IAdminOverviewService _adminOverviewService;
        private readonly IProductoQueryService _productoQueryService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IAdminOverviewService adminOverviewService, IProductoQueryService productoQueryService, IMemoryCache cache)
        {
            _logger = logger;
            _adminOverviewService = adminOverviewService;
            _productoQueryService = productoQueryService;
            _cache = cache;
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
            // casi de una y su cuerpo completo llega en UN fetch (la acción
            // TodasLasSecciones de acá abajo). El reparto por data-seccion y los
            // esqueletos son maquinaria de site.js con data-* de la vista.
            // HomeController es el único controller mixto (público + admin): el
            // layout lo declara el action acá — la clase no hereda de una base.
            ViewData[ViewDataKeys.Layout] = LayoutKeys.Admin;
            return View(new HomeAdminViewModel());
        }

        // TODO DE GOLPE: el caparazón pide su cuerpo completo en UN fetch
        // (admin/secciones/todas). La respuesta es el envase con TODAS las
        // secciones; el loader de site.js reparte por data-seccion y pinta el
        // panel junto. La base corre sus consultas UNA vez por carga del panel
        // (el overview vive 30s en cache).
        [Route("admin/secciones/todas")]
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> TodasLasSecciones()
        {
            var cancelado = HttpContext.RequestAborted;
            var datos = await _adminOverviewService.ObtenerResumenHomeAsync(cancelado);
            var estancados = await _adminOverviewService.ObtenerPedidosEstancadosPaginadoAsync(1, ct: cancelado);

            // Envase = el mismo HomeAdminViewModel del caparazón: cada pieza
            // sale rendereada por su parcial, con la clave data-seccion arriba.
            var modelo = new HomeAdminViewModel
            {
                EstaDbOnline = datos.DbOnline,
                VersionApp = datos.AppVersion,
                ResumenDiario = new ResumenDiarioViewModel
                {
                    VentasHoy = datos.ResumenDiario.VentasHoy,
                    PorcentajeVentas = datos.ResumenDiario.PorcentajeVentas,
                    PedidosHoy = datos.ResumenDiario.PedidosHoy,
                    PorcentajePedidos = datos.ResumenDiario.PorcentajePedidos,
                    EnviadosHoy = datos.ResumenDiario.EnviadosHoy,
                    PorcentajeEnviados = datos.ResumenDiario.PorcentajeEnviados,
                    StockBajo = datos.ResumenDiario.StockBajo
                },
                UltimosCambios = datos.UltimosCambios.Select(a => new AuditoriaItemViewModel
                {
                    Usuario = a.UsuarioNombre,
                    Accion = a.Accion,
                    Fecha = a.Fecha
                }).ToList(),
                UltimosMovimientosStock = datos.UltimosMovimientosStock.Select(m => new MovimientoStockItemViewModel
                {
                    ProductoNombre = m.ProductoNombre,
                    Cantidad = m.Cantidad,
                    TipoMovimientoId = m.TipoMovimientoId,
                    Fecha = m.Fecha,
                    Observaciones = m.Observaciones
                }).ToList(),
                ProductosBajoStock = datos.ProductosBajoStock.Select(p => new ProductoBajoStockItemViewModel
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    ImagenUrl = p.ImagenUrl,
                    Categoria = p.Categoria,
                    Stock = p.Stock
                }).ToList(),
                PedidosRecientes = datos.PedidosRecientes.Select(p => new PedidoRecienteItemViewModel
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    Cliente = p.Cliente,
                    Total = p.Total,
                    EstadoPedido = p.EstadoPedido,
                    EstadoPedidoId = p.EstadoPedidoId
                }).ToList(),
                PaginaActual = estancados.PaginaActual,
                TotalPaginas = estancados.TotalPaginas,
                TotalRegistros = estancados.TotalRegistros,
                PedidosEstancados = estancados.Pedidos.Select(p => new PedidoEstancadoViewModel
                {
                    PedidoId = p.PedidoId,
                    Cliente = p.ClienteNombre,
                    Fecha = p.Fecha,
                    HorasTranscurridas = p.HorasTranscurridas
                }).ToList()
            };

            return PartialView("_SeccionesDelPanel", modelo);
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

        // ------------------------------------------------------------------
        // SEO: sitemap.xml y robots.txt
        // ------------------------------------------------------------------

        // Literal "sitemap.xml": los bots lo buscan en la raíz. El contenido vive
        // 30 min en cache (una query liviana de ids por media hora en vez de por
        // request); UseStaticFiles corre antes del routing, así que un robots.txt
        // físico en wwwroot siempre podría pisar esta acción sin deploy.
        [AllowAnonymous]
        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Sitemap()
        {
            var xml = await _cache.GetOrCreateAsync("sitemap-xml", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                var baseUri = $"{Request.Scheme}://{Request.Host}";
                var ids = await _productoQueryService.ObtenerIdsProductosActivosAsync(HttpContext.RequestAborted);

                var sb = new StringBuilder()
                    .AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                    .AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

                // Home y catálogo + la única página legal que existe (los vencidos
                // dan 404 y ensucian el índice). Producto no tiene fecha de alta:
                // el lastmod queda fuera (el schema lo permite).
                foreach (var ruta in new[] { "/", "/Productos", "/Home/Privacy" })
                {
                    sb.AppendLine($"  <url><loc>{baseUri}{ruta}</loc></url>");
                }

                foreach (var id in ids)
                {
                    // La ficha no lleva {id} en la ruta: es query (ProductosController).
                    sb.AppendLine($"  <url><loc>{baseUri}/Productos/Detalles?id={id}</loc></url>");
                }

                sb.Append("</urlset>");
                return sb.ToString();
            });

            return Content(xml, "application/xml");
        }

        [AllowAnonymous]
        [HttpGet("robots.txt")]
        public IActionResult Robots()
        {
            var baseUri = $"{Request.Scheme}://{Request.Host}";
            var cuerpo = new StringBuilder()
                .AppendLine("User-agent: *")
                .AppendLine("Disallow: /admin")      // panel, ofertas, envíos y secciones
                .AppendLine("Disallow: /Admin")      // AdminUsuarios y AdminPedidos (matchea por prefijo)
                .AppendLine("Disallow: /hangfire")
                .AppendLine("Disallow: /Accounts")   // login/registro/reset no son contenido
                .AppendLine("Disallow: /Carrito")
                .AppendLine("Disallow: /Checkout")
                .AppendLine("Disallow: /Pedidos")
                .AppendLine("Disallow: /Repartidor")
                .AppendLine($"Sitemap: {baseUri}/sitemap.xml")
                .ToString();

            return Content(cuerpo, "text/plain");
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
