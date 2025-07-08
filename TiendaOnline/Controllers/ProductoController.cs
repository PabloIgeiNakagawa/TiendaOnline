using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;

public class ProductoController : Controller
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;

    public ProductoController(IProductoService productoService, ICategoriaService categoriaService)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
    }

    public async Task<IActionResult> Index(string? busqueda)
    {
        var productos = await _productoService.ObtenerProductosAsync();
        var categorias = await _categoriaService.ObtenerCategoriasAsync();

        ViewBag.Categorias = categorias;
        ViewBag.Busqueda = busqueda;
        return View(productos);
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id)
    {
        var producto = await _productoService.ObtenerProductoAsync(id);
        return View(producto);
    }

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AgregarProducto()
    {
        ViewBag.Categorias = await _categoriaService.ObtenerCategoriasAsync();
        return View(new Producto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AgregarProducto(Producto producto, IFormFile ImagenArchivo)
    {
        if (ImagenArchivo == null || ImagenArchivo.Length == 0)
        {
            ModelState.AddModelError("ImagenArchivo", "La imagen es obligatoria.");
            return View(producto);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = await _categoriaService.ObtenerCategoriasAsync();
            return View(producto);
        }
        using var ms = new MemoryStream();
        await ImagenArchivo.CopyToAsync(ms);
        producto.Imagen = ms.ToArray();

        await _productoService.AgregarProductoAsync(producto);

        return RedirectToAction("Detalles", "Producto", new { id = producto.ProductoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DarAltaProducto(int id)
    {
        await _productoService.DarAltaProductoAsync(id);
        TempData["MensajeExito"] = "El producto se dió de alta correctamente.";
        return RedirectToAction("Productos", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DarBajaProducto(int id)
    {
        await _productoService.DarBajaProductoAsync(id);
        TempData["MensajeExito"] = "El producto se dió de baja correctamente.";
        return RedirectToAction("Productos", "Admin");
    }

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarProducto(int id)
    {
        var producto = await _productoService.ObtenerProductoAsync(id);
        ViewBag.Categorias = await _categoriaService.ObtenerCategoriasAsync();
        return View(producto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarProducto(int id, Producto productoEditado, IFormFile ImagenArchivo)
    {
        await _productoService.EditarProductoAsync(id, productoEditado, ImagenArchivo);
        TempData["MensajeExito"] = "El producto se actualizó correctamente.";
        return RedirectToAction("Detalles", "Producto", new { id });
    }
}

