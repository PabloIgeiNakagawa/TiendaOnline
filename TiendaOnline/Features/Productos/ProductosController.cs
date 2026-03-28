using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Productos.Queries;

namespace TiendaOnline.Features.Productos
{
    [Route("[controller]")]
    public class ProductosController : Controller
    {
        private readonly IProductoQueryService _productoQueryService;
        private readonly ICategoriaQueryService _categoriaQueryService;

        public ProductosController(IProductoQueryService productoQueryService, ICategoriaQueryService categoriaQueryService)
        {
            _productoQueryService = productoQueryService;
            _categoriaQueryService = categoriaQueryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ObtenerProductosCatalogoRequest request)
        {
            var productosPaginados = await _productoQueryService.ObtenerProductosCatalogoAsync(request);

            var categoriasRaiz = await _categoriaQueryService.ObtenerCategoriasRaizAsync();

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

