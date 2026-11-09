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
            var productosPaginados = await _productoQueryService.ObtenerProductosCatalogoAsync(request, HttpContext.RequestAborted);

            var categoriasRaiz = await _categoriaQueryService.ObtenerCategoriasRaizAsync(HttpContext.RequestAborted);

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

        // Sugerencias del buscador de la navbar: la misma consulta del catálogo
        // (busca por nombre/descripción) recortada a 6. Devuelve HTML armado por
        // Razor (auto-escapado) que el JS de _TiendaHeader pega debajo del input.
        [HttpGet("Sugerencias")]
        public async Task<IActionResult> Sugerencias([FromQuery] string? busqueda)
        {
            var termino = busqueda?.Trim();
            if (string.IsNullOrEmpty(termino) || termino.Length < 2)
                return Content(string.Empty);

            if (termino.Length > 100) termino = termino[..100];

            var resultado = await _productoQueryService.ObtenerProductosCatalogoAsync(
                new ObtenerProductosCatalogoRequest { Busqueda = termino, Cantidad = 6 },
                HttpContext.RequestAborted);

            return PartialView("_SugerenciasBusqueda", resultado.Items.ToList());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Detalles(int id)
        {
            if (id <= 0)
                return BadRequest();

            var producto = await _productoQueryService.ObtenerProductoAsync(id, HttpContext.RequestAborted);

            if (producto is null)
                return NotFound();

            var vm = new DetallesViewModel
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                PrecioOferta = producto.PrecioOferta,
                FechaFinOferta = producto.FechaFinOferta,
                Stock = producto.Stock,
                ImagenUrl = producto.ImagenUrl,
                CategoriaNombre = producto.CategoriaNombre,
                CategoriaId = producto.CategoriaId
            };
            return View(vm);
        }
    }
}

