using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.IServices;
using TiendaOnline.Models;

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

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AgregarProducto()
    {
        var categoriasHoja = await _categoriaService.ObtenerCategoriasHojaAsync();

        var listaFormateada = categoriasHoja.Select(c => new {
            Id = c.CategoriaId,
            NombreRuta = ObtenerRutaCompleta(c)
        });

        ViewBag.Categorias = new SelectList(listaFormateada, "Id", "NombreRuta");
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
        }

        if (!await _categoriaService.EsCategoriaHojaAsync(producto.CategoriaId))
        {
            ModelState.AddModelError("CategoriaId", "Debe seleccionar una subcategoría final.");
        }

        if (!ModelState.IsValid)
        {
            var categoriasHoja = await _categoriaService.ObtenerCategoriasHojaAsync();
            var listaParaSelect = categoriasHoja.Select(c => new {
                IdValue = c.CategoriaId,
                TextValue = ObtenerRutaCompleta(c)
            }).ToList();

            ViewBag.Categorias = new SelectList(listaParaSelect, "IdValue", "TextValue");
            return View(producto);
        }

        string urlImagen = await _imagenService.SubirImagenAsync(ImagenArchivo);
        producto.ImagenUrl = urlImagen;

        await _productoService.AgregarProductoAsync(producto);
        return RedirectToAction("Detalles", new { id = producto.ProductoId });
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

    private string ObtenerRutaCompleta(Categoria cat)
    {
        if (cat.CategoriaPadre == null) return cat.Nombre;
        return $"{ObtenerRutaCompleta(cat.CategoriaPadre)} > {cat.Nombre}";
    }
}

