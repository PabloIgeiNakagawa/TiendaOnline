using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Services.IServices;
using TiendaOnline.Areas.Admin.ViewModels.Producto;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productos = await _productoService.ObtenerProductosAsync();
            var categorias = await _categoriaService.ObtenerCategoriasAsync();

            ViewBag.Categorias = categorias;
            return View(productos);
        }

        [HttpGet]
        public async Task<IActionResult> Agregar()
        {
            await CargarCategoriasEnViewBag();
            return View(new AgregarProductoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(AgregarProductoViewModel model)
        {
            if (!await _categoriaService.EsCategoriaHojaAsync(model.CategoriaId))
            {
                ModelState.AddModelError("CategoriaId", "Debe seleccionar una subcategoría final.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCategoriasEnViewBag();
                return View(model);
            }

            string urlImagen = "/img/no-image.png";
            if (model.ImagenArchivo != null)
            {
                using var stream = model.ImagenArchivo.OpenReadStream();
                urlImagen = await _imagenService.SubirImagenAsync(stream, model.ImagenArchivo.FileName);
            }

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
            TempData["MensajeExito"] = "Producto creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _productoService.DarAltaProductoAsync(id);
            TempData["MensajeExito"] = "Producto activado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _productoService.DarBajaProductoAsync(id);
            TempData["MensajeExito"] = "Producto desactivado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _productoService.ObtenerProductoAsync(id);
            if (producto == null) return NotFound();

            await CargarCategoriasEnViewBag();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Producto producto, IFormFile? ImagenArchivo)
        {
            Stream? stream = null;
            string? nombre = null;

            if (ImagenArchivo != null)
            {
                stream = ImagenArchivo.OpenReadStream();
                nombre = ImagenArchivo.FileName;
            }

            await _productoService.EditarProductoAsync(id, producto, stream, nombre);
            TempData["MensajeExito"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCategoriasEnViewBag()
        {
            var categoriasHoja = await _categoriaService.ObtenerCategoriasHojaAsync();
            var listaFormateada = categoriasHoja.Select(c => new {
                Id = c.CategoriaId,
                NombreRuta = ObtenerRutaCompleta(c)
            });
            ViewBag.Categorias = new SelectList(listaFormateada, "Id", "NombreRuta");
        }

        private string ObtenerRutaCompleta(Categoria cat)
        {
            if (cat.CategoriaPadre == null) return cat.Nombre;
            return $"{ObtenerRutaCompleta(cat.CategoriaPadre)} > {cat.Nombre}";
        }
    }
}