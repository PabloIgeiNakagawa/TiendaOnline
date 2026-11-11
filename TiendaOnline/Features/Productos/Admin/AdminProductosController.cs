using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.Productos.Admin
{
    [Route("admin/productos")]
    [Authorize(Roles = "Administrador")]
    public class AdminProductosController : AdminControllerBase
    {
        private readonly IProductoCommandService _productoCommandService;
        private readonly IProductoQueryService _productoQueryService;
        private readonly ICategoriaQueryService _categoriaQueryService;
        private readonly ILogger<AdminProductosController> _logger;

        public AdminProductosController(IProductoCommandService productoCommandService, IProductoQueryService productoQueryService, ICategoriaQueryService categoriaQueryService, ILogger<AdminProductosController> logger)
        {
            _productoCommandService = productoCommandService;
            _productoQueryService = productoQueryService;
            _categoriaQueryService = categoriaQueryService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Catalogo([FromQuery] ObtenerProductosAdminRequest request)
        {
            // Obtenemos los productos paginados y filtrados
            var pagedResult = await _productoQueryService.ObtenerProductosAdminAsync(request, HttpContext.RequestAborted);

            // Obtenemos las categorías para el select (usando DTO)
            var categoriasEntidad = await _categoriaQueryService.ObtenerCategoriasHojaAsync(HttpContext.RequestAborted);
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
                CategoriaId = request.CategoriaId,
                Estado = request.Estado,
                Stock = request.Stock,
                TotalActivos = pagedResult.Items.Count(x => x.Activo),
                TotalInactivos = pagedResult.Items.Count(x => !x.Activo)
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> AgregarProducto()
        {
            var model = new AgregarProductoViewModel
            {
                Categorias = await ObtenerListaCategoriasAsync()
            };

            return View(model);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(AgregarProductoViewModel model)
        {
            if (!await _categoriaQueryService.EsCategoriaHojaAsync(model.CategoriaId, HttpContext.RequestAborted))
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

            await _productoCommandService.AgregarProductoAsync(dto, HttpContext.RequestAborted);
            _logger.LogInformation("Producto creado: {Nombre} (categoría {CategoriaId}, stock {Stock}, precio {Precio}) por {Admin}",
                dto.Nombre, dto.CategoriaId, dto.Stock, dto.Precio, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Producto creado correctamente.");
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> EditarProducto(int id)
        {
            var producto = await _productoQueryService.ObtenerProductoAsync(id, HttpContext.RequestAborted);
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

            await _productoCommandService.EditarProductoAsync(dto, HttpContext.RequestAborted);
            _logger.LogInformation("Producto {ProductoId} editado por {Admin}", dto.ProductoId, User.FindFirstValue(ClaimTypes.NameIdentifier));

            TempData.SetMensajeExito("Producto actualizado correctamente.");
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarAlta(int id)
        {
            await _productoCommandService.DarAltaProductoAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Producto {ProductoId} activado por {Admin}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Producto activado.");
            return RedirectToAction(nameof(Catalogo));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarBaja(int id)
        {
            await _productoCommandService.DarBajaProductoAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Producto {ProductoId} desactivado por {Admin}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Producto desactivado.");
            return RedirectToAction(nameof(Catalogo));
        }
        private async Task<IEnumerable<SelectListItem>> ObtenerListaCategoriasAsync()
        {
            var categoriasHoja = await _categoriaQueryService.ObtenerCategoriasHojaAsync(HttpContext.RequestAborted);

            return categoriasHoja.Select(c => new SelectListItem
            {
                Value = c.CategoriaId.ToString(),
                Text = ObtenerRutaCompleta(c) // Tu lógica de nombres jerárquicos
            });
        }

        private static string ObtenerRutaCompleta(CategoriaDto cat)
        {
            if (cat.CategoriaPadre == null) return cat.Nombre;
            return $"{ObtenerRutaCompleta(cat.CategoriaPadre)} > {cat.Nombre}";
        }
    }
}