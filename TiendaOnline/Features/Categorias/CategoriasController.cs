using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Categorias.Commands;
using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Categorias
{
    [Route("admin/categorias")]
    [Authorize(Roles = "Administrador")]
    public class CategoriasController : Controller
    {
        private readonly ICategoriaQueryService _categoriaQueryService;
        private readonly ICategoriaCommandService _categoriaCommandService;
        private readonly ILogger<CategoriasController> _logger;

        public CategoriasController(ICategoriaQueryService categoriaQueryService, ICategoriaCommandService categoriaCommandService, ILogger<CategoriasController> logger)
        {
            _categoriaQueryService = categoriaQueryService;
            _categoriaCommandService = categoriaCommandService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(FiltrosListadoViewModel filtros)
        {
            var pagedResult = await _categoriaQueryService.ObtenerCategoriasPaginadasAsync(filtros.Pagina, filtros.TamanoPagina, filtros.Busqueda, filtros.Nivel, HttpContext.RequestAborted);

            var viewModel = new CategoriaListadoViewModel
            {
                Paginacion = pagedResult,
                Busqueda = filtros.Busqueda,
                Nivel = filtros.Nivel
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> AgregarCategoria()
        {
            ViewBag.Categorias = new SelectList(await _categoriaQueryService.ObtenerCategoriasAsync(HttpContext.RequestAborted), "CategoriaId", "Nombre");
            return View();
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(AgregarCategoriaViewModel categoria)
        {
            if (ModelState.IsValid)
            {
                if (await _categoriaQueryService.ExisteNombreAsync(categoria.Nombre, HttpContext.RequestAborted))
                {
                    ModelState.AddModelError("Nombre", "Ya existe una categoría con este nombre.");
                }
                else
                {
                    var dto = new CategoriaDto
                    {
                        Nombre = categoria.Nombre,
                        CategoriaPadreId = categoria.CategoriaPadreId
                    };
                    await _categoriaCommandService.AgregarCategoriaAsync(dto, HttpContext.RequestAborted);
                    _logger.LogInformation("Categoría creada: {Nombre} (padre {CategoriaPadreId}) por {Admin}",
                        dto.Nombre, dto.CategoriaPadreId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                    TempData.SetMensajeExito("La categoría se creó correctamente.");
                    return RedirectToAction(nameof(Listado));
                }
            }
            ViewBag.Categorias = new SelectList(await _categoriaQueryService.ObtenerCategoriasAsync(HttpContext.RequestAborted), "CategoriaId", "Nombre");
            return View(categoria);
        }

        /*[HttpGet("[action]")]
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _categoriaQueryService.ObtenerCategoriaAsync(id);
            if (categoria == null) return NotFound();

            // Filtramos para que no pueda elegirse a sí misma como padre en el Select de la vista
            var todas = await _categoriaQueryService.ObtenerCategoriasAsync();
            ViewBag.Categorias = new SelectList(todas.Where(c => c.CategoriaId != id), "CategoriaId", "Nombre", categoria.CategoriaPadreId);

            return View(categoria);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarCategoriaViewModel categoria)
        {
            try
            {
                await _categoriaCommandService.EditarCategoriaAsync(categoria.CategoriaId, categoria.Nombre);

                await _categoriaCommandService.CambiarCategoriaPadre(categoria.CategoriaId, categoria.CategoriaPadreId);

                return RedirectToAction(nameof(Listado));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var todas = await _categoriaQueryService.ObtenerCategoriasAsync();
                ViewBag.Categorias = new SelectList(todas.Where(c => c.CategoriaId != categoria.CategoriaId), "CategoriaId", "Nombre");
                return View(categoria);
            }
        }*/

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            bool eliminado = await _categoriaCommandService.EliminarCategoriaAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Eliminación de categoría {CategoriaId} por {Admin} (resultado: {Eliminado})",
                id, User.FindFirstValue(ClaimTypes.NameIdentifier), eliminado);
            if (!eliminado)
            {
                TempData.SetMensajeError("No se puede eliminar: la categoría tiene productos o subcategorías.");
            }
            return RedirectToAction(nameof(Listado));
        }
    }
}