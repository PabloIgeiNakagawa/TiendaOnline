using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.Categorias;

namespace TiendaOnline.Features.Admin.Productos
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class ProductosController : Controller
    {
        private readonly IProductoCommandService _productoCommandService;
        private readonly IProductoQueryService _productoQueryService;
        private readonly ICategoriaService _categoriaService;

        public ProductosController(IProductoCommandService productoCommandService, IProductoQueryService productoQueryService, ICategoriaService categoriaService)
        {
            _productoCommandService = productoCommandService;
            _productoQueryService = productoQueryService;
            _categoriaService = categoriaService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Catalogo([FromQuery] ObtenerProductosAdminRequest request)
        {
            ViewData["Title"] = "Catálogo";

            // Obtenemos los productos paginados y filtrados
            var pagedResult = await _productoQueryService.ObtenerProductosAdminAsync(request);

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
                Busqueda = request.Busqueda,
                CategoriaSeleccionada = request.CategoriaId,
                EstadoSeleccionado = request.Estado,
                StockSeleccionado = request.Stock,
                TotalActivos = pagedResult.Items.Count(x => x.Activo),
                TotalInactivos = pagedResult.Items.Count(x => !x.Activo)
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> AgregarProducto()
        {
            ViewData["Title"] = "Agregar Producto";

            var model = new AgregarProductoViewModel
            {
                Categorias = await ObtenerListaCategoriasAsync()
            };

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
                // RE-CARGAMOS las categorías antes de volver a la vista
                model.Categorias = await ObtenerListaCategoriasAsync();
                return View(model);
            }

            var dto = new AgregarProductoDto
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Precio = model.Precio,
                Stock = model.Stock,
                CategoriaId = model.CategoriaId,
                ImagenStream = model.ImagenArchivo?.OpenReadStream(),
                NombreArchivo = model.ImagenArchivo?.FileName
            };

            await _productoCommandService.AgregarProductoAsync(dto);
            TempData["MensajeExito"] = "Producto creado correctamente.";
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> EditarProducto(int id)
        {
            var producto = await _productoQueryService.ObtenerProductoAsync(id);
            if (producto == null) return NotFound();

            var model = new EditarProductoViewModel
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                CategoriaId = producto.CategoriaId,
                ImagenUrlActual = producto.ImagenUrl,
                Categorias = await ObtenerListaCategoriasAsync()
            };

            return View(model);
        }

        [HttpPost("[action]/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProducto(EditarProductoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // IMPORTANTE: Si volvemos a la vista, hay que rellenar la lista
                model.Categorias = await ObtenerListaCategoriasAsync();
                return View(model);
            }

            var dto = new EditarProductoDto
            {
                ProductoId = model.ProductoId,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Precio = model.Precio,
                CategoriaId = model.CategoriaId,
                ImagenStream = model.ImagenArchivo?.OpenReadStream(),
                NombreArchivo = model.ImagenArchivo?.FileName
            };

            await _productoCommandService.EditarProductoAsync(dto);

            TempData["MensajeExito"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _productoCommandService.DarAltaProductoAsync(id);
            TempData["MensajeExito"] = "Producto activado.";
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _productoCommandService.DarBajaProductoAsync(id);
            TempData["MensajeExito"] = "Producto desactivado.";
            return RedirectToAction(nameof(Catalogo));
        }
        private async Task<IEnumerable<SelectListItem>> ObtenerListaCategoriasAsync()
        {
            var categoriasHoja = await _categoriaService.ObtenerCategoriasHojaAsync();

            return categoriasHoja.Select(c => new SelectListItem
            {
                Value = c.CategoriaId.ToString(),
                Text = ObtenerRutaCompleta(c) // Tu lógica de nombres jerárquicos
            });
        }

        private string ObtenerRutaCompleta(Categoria cat)
        {
            if (cat.CategoriaPadre == null) return cat.Nombre;
            return $"{ObtenerRutaCompleta(cat.CategoriaPadre)} > {cat.Nombre}";
        }
    }
}