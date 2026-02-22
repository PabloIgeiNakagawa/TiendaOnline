using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Features.Admin.Categorias;

namespace TiendaOnline.Features.Tienda.Productos
{
    [Route("[controller]")]
    public class ProductosController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        public ProductosController(IProductoService productoService, ICategoriaService categoriaService)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string busqueda, int? categoriaId, decimal? min, decimal? max, string orden, int pagina = 1)
        {
            ViewData["Title"] = "Catálogo de Productos";

            int registrosPorPagina = 18;

            var productosPaginados = await _productoService.ObtenerProductosTiendaPaginadoAsync(
                busqueda, categoriaId, min, max, orden, pagina, registrosPorPagina);

            var viewModel = new ProductoIndexViewModel
            {
                Paginacion = productosPaginados,
                Busqueda = busqueda,
                CategoriaId = categoriaId,
                PrecioMin = min,
                PrecioMax = max,
                Orden = orden,
                CategoriasRaiz = (List<TiendaOnline.Domain.Entities.Categoria>)await _categoriaService.ObtenerCategoriasRaizAsync()
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Detalles(int id)
        {
            var producto = await _productoService.ObtenerProductoAsync(id);
            ViewData["Title"] = producto?.Nombre;
            return View(producto);
        }
    }
}

