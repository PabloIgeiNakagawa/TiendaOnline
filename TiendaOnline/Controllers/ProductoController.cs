using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.ViewModels.Producto;

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
        return View(new AgregarProductoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AgregarProducto(AgregarProductoViewModel model)
    {
        // 1. Validar regla de negocio (que tenga padre)
        if (!await _categoriaService.EsCategoriaHojaAsync(model.CategoriaId))
        {
            ModelState.AddModelError("CategoriaId", "Debe seleccionar una subcategoría final.");
        }

        if (!ModelState.IsValid)
        {
            // Recargar el ViewBag como ya lo hacías
            var categoriasHoja = await _categoriaService.ObtenerCategoriasHojaAsync();
            ViewBag.Categorias = new SelectList(categoriasHoja.Select(c => new {
                IdValue = c.CategoriaId,
                TextValue = ObtenerRutaCompleta(c)
            }), "IdValue", "TextValue");

            return View(model);
        }

        // 2. Mapear del ViewModel a la Entidad real
        string urlImagen = await _imagenService.SubirImagenAsync(model.ImagenArchivo);

        var nuevoProducto = new Producto
        {
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            Precio = model.Precio,
            Stock = model.Stock,
            CategoriaId = model.CategoriaId,
            ImagenUrl = urlImagen,
            Activo = true
        };

        await _productoService.AgregarProductoAsync(nuevoProducto);
        return RedirectToAction("Detalles", new { id = nuevoProducto.ProductoId });
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

