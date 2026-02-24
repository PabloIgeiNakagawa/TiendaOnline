using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Features.Admin.Categorias;

namespace TiendaOnline.Features.Tienda.Productos
{
    [Route("[controller]")]
    public class ProductosController : Controller
    {
        private readonly IProductoQueryService _productoQueryService;
        private readonly ICategoriaService _categoriaService;

        public ProductosController(IProductoQueryService productoQueryService, ICategoriaService categoriaService)
        {
            _productoQueryService = productoQueryService;
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ObtenerProductosCatalogoRequest request)
        {
            ViewData["Title"] = "Catálogo de Productos";

            var productosPaginados = await _productoQueryService.ObtenerProductosCatalogoAsync(request);

            var categoriasRaiz = await _categoriaService.ObtenerCategoriasRaizAsync();

            var viewModel = new ProductoIndexViewModel
            {
                Paginacion = productosPaginados,
                Busqueda = request.Busqueda,
                CategoriaId = request.CategoriaId,
                PrecioMin = request.PrecioMin,
                PrecioMax = request.PrecioMax,
                Orden = request.Orden,
                CategoriasRaiz = categoriasRaiz.ToList()
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Detalles(int id)
        {
            if (id <= 0)
                return BadRequest();

            var producto = await _productoQueryService.ObtenerProductoAsync(id);

            if (producto is null)
                return NotFound();

            ViewData["Title"] = producto.Nombre;

            var vm = new DetallesViewModel
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock,
                ImagenUrl = producto.ImagenUrl,
                CategoriaNombre = producto.CategoriaNombre,
                CategoriaId = producto.CategoriaId
            };
            return View(vm);
        }
    }
}

