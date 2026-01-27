using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Services.IServices;

public class AdminController : Controller
{
    private readonly IProductoService _productoService;
    private readonly IUsuarioService _usuarioService;
    private readonly ICategoriaService _categoriaService;
    private readonly IPedidoService _pedidoService;
    private readonly IReportesService _reportesService;
    private readonly IAuditoriaService _auditoriaService;

    public AdminController(IProductoService productoService, IUsuarioService usuarioService, ICategoriaService categoriaService, IPedidoService pedidoService, IReportesService reportesService, IAuditoriaService auditoriaService)
    {
        _productoService = productoService;
        _usuarioService = usuarioService;
        _categoriaService = categoriaService;
        _pedidoService = pedidoService;
        _reportesService = reportesService;
        _auditoriaService = auditoriaService;
    }

    // Panel principal
    [Authorize(Roles = "Administrador")]
    public IActionResult Index()
    {
        return View();
    }

    // Usuarios
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _usuarioService.ObtenerUsuariosAsync();
        return View(usuarios);
    }

    // Productos
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Productos()
    {
        var productos = await _productoService.ObtenerProductosAsync();
        var categorias = await _categoriaService.ObtenerCategoriasAsync();

        ViewBag.Categorias = categorias;
        return View(productos);
    }

    // Categorias
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Categorias()
    {
        var categorias = await _categoriaService.ObtenerCategoriasAsync();
        return View(categorias);
    }

    // Pedidos
    [HttpGet]
    [Authorize(Roles = "Administrador, Repartidor")]
    public async Task<IActionResult> Pedidos()
    {
        var pedidos = await _pedidoService.ObtenerPedidosConDetallesAsync();
        return View(pedidos);
    }

    // Reportes
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Reportes()
    {
        var model = await _reportesService.ObtenerDashboardAsync(0);
        return View(model);
    }

    // Datos del dashboard en formato JSON para gráficos
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DatosDashboardJson(int periodo)
    {
        var model = await _reportesService.ObtenerDashboardAsync(periodo);
        return Json(model);
    }

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Auditorias()
    {
        var auditorias = await _auditoriaService.ObtenerAuditoriasAsync();
        return View(auditorias);
    }
}
