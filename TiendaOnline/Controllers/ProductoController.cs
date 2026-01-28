using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Services.IServices;

public class ProductoController : Controller
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IImagenService _imagenService;

    public ProductoController(IProductoService productoService, ICategoriaService categoriaService, IImagenService imagenService)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _imagenService = imagenService;
    }

    public async Task<IActionResult> Index(string busqueda)
    {
        var categoriasRaiz = await _categoriaService.ObtenerCategoriasRaizAsync();
        var productos = await _productoService.ObtenerProductosAsync();

        ViewBag.CategoriasRaiz = categoriasRaiz;
        ViewBag.Busqueda = busqueda; 

        return View(productos);
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id)
    {
        var producto = await _productoService.ObtenerProductoAsync(id);
        return View(producto);
    }
}

