using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Categorias.Commands;
using TiendaOnline.Application.Categorias.Common;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.Categorias
{
    [Route("admin/categorias")]
    [Authorize(Roles = "Administrador")]
    public class CategoriasController : AdminControllerBase
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

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _categoriaCommandService.EliminarCategoriaAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Categoría {CategoriaId} eliminada por {Admin}",
                id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("La categoría se eliminó correctamente.");
            return RedirectToAction(nameof(Listado));
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _categoriaQueryService.ObtenerCategoriaPorIdAsync(id, HttpContext.RequestAborted);
            if (categoria is null)
                return NotFound();

            ViewBag.Categorias = new SelectList(
                (await _categoriaQueryService.ObtenerCategoriasAsync(HttpContext.RequestAborted))
                    .Where(c => c.CategoriaId != id),
                "CategoriaId", "Nombre");

            return View(new EditarCategoriaViewModel
            {
                CategoriaId = categoria.CategoriaId,
                Nombre = categoria.Nombre,
                CategoriaPadreId = categoria.CategoriaPadreId
            });
        }

        [HttpPost("[action]/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EditarCategoriaViewModel model)
        {
            var categoria = await _categoriaQueryService.ObtenerCategoriaPorIdAsync(id, HttpContext.RequestAborted);
            if (categoria is null)
                return NotFound();

            if (ModelState.IsValid)
            {
                // El nombre debe seguir siendo único entre las OTRAS categorías
                // (la propia ya tiene ese nombre y no es conflicto).
                if (await _categoriaQueryService.ExisteNombreEnOtraCategoriaAsync(model.Nombre, id, HttpContext.RequestAborted))
                {
                    ModelState.AddModelError("Nombre", "Ya existe una categoría con este nombre.");
                }
                else
                {
                    // Un bucle de jerarquía sale como CategoriaNoValidaException: el
                    // DomainExceptionFilter global lo convierte en aviso con redirect.
                    await _categoriaCommandService.EditarCategoriaAsync(id, model.Nombre, model.CategoriaPadreId, HttpContext.RequestAborted);
                    _logger.LogInformation("Categoría {CategoriaId} editada (nombre {Nombre}, padre {CategoriaPadreId}) por {Admin}",
                        id, model.Nombre, model.CategoriaPadreId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                    TempData.SetMensajeExito("La categoría se actualizó correctamente.");
                    return RedirectToAction(nameof(Listado));
                }
            }

            ViewBag.Categorias = new SelectList(
                (await _categoriaQueryService.ObtenerCategoriasAsync(HttpContext.RequestAborted))
                    .Where(c => c.CategoriaId != id),
                "CategoriaId", "Nombre");
            return View(model);
        }
    }
}