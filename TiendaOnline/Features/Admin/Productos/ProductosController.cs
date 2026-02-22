using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Features.Admin.Categorias;
using TiendaOnline.Features.Tienda.Productos;

namespace TiendaOnline.Features.Admin.Productos
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class ProductosController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;
        private readonly IImagenService _imagenService;

        public ProductosController(IProductoService productoService, ICategoriaService categoriaService, IImagenService imagenService)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
            _imagenService = imagenService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Catalogo(string? busqueda, int? categoriaId, string? estado, string? stock, int pagina = 1, int tamanoPagina = 10)
        {
            ViewData["Title"] = "Catálogo";

            if (pagina < 1) pagina = 1;
            if (tamanoPagina < 1) tamanoPagina = 10;

            // Obtenemos los productos paginados y filtrados
            var pagedResult = await _productoService.ObtenerProductosPaginadosAsync(
                busqueda, categoriaId, estado, stock, pagina, tamanoPagina);

            // Obtenemos las categorías para el select (usando DTO)
            var categoriasEntidad = await _categoriaService.ObtenerCategoriasHojaAsync();
            var categoriasDto = categoriasEntidad.Select(c => new CategoriaDto
            {
                CategoriaId = c.CategoriaId,
                Nombre = c.Nombre
            }).ToList();

            var viewModel = new ProductoCatalogoViewModel
            {
                ProductosPaginados = pagedResult,
                Categorias = categoriasDto,
                Busqueda = busqueda,
                CategoriaSeleccionada = categoriaId,
                EstadoSeleccionado = estado,
                StockSeleccionado = stock,
                TotalActivos = pagedResult.Items.Count(x => x.Activo),
                TotalInactivos = pagedResult.Items.Count(x => !x.Activo)
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> AgregarProducto()
        {
            ViewData["Title"] = "Agregar Producto";
            await CargarCategoriasEnViewBag();
            return View(new AgregarProductoViewModel());
        }

        [HttpPost("[action]")]
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
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _productoService.DarAltaProductoAsync(id);
            TempData["MensajeExito"] = "Producto activado.";
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _productoService.DarBajaProductoAsync(id);
            TempData["MensajeExito"] = "Producto desactivado.";
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> EditarProducto(int id)
        {
            var producto = await _productoService.ObtenerProductoAsync(id);
            if (producto == null) return NotFound();

            await CargarCategoriasEnViewBag();
            return View(producto);
        }

        [HttpPost("[action]")]
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
            return RedirectToAction(nameof(Catalogo));
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