using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class CategoriaController : Controller
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> Listado()
        {
            var categorias = await _categoriaService.ObtenerCategoriasAsync();
            return View(categorias);
        }

        [HttpGet]
        public async Task<IActionResult> AgregarCategoria()
        {
            ViewBag.Categorias = new SelectList(await _categoriaService.ObtenerCategoriasAsync(), "CategoriaId", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                if (await _categoriaService.ExisteNombreAsync(categoria.Nombre))
                {
                    ModelState.AddModelError("Nombre", "Ya existe una categoría con este nombre.");
                }
                else
                {
                    await _categoriaService.AgregarCategoriaAsync(categoria);
                    TempData["MensajeExito"] = "La categoría se creó correctamente.";
                    return RedirectToAction("Categorias", "Admin");
                }
            }
            ViewBag.Categorias = new SelectList(await _categoriaService.ObtenerCategoriasAsync(), "CategoriaId", "Nombre");
            return View(categoria);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _categoriaService.ObtenerCategoriaAsync(id);
            if (categoria == null) return NotFound();

            // Filtramos para que no pueda elegirse a sí misma como padre en el Select de la vista
            var todas = await _categoriaService.ObtenerCategoriasAsync();
            ViewBag.Categorias = new SelectList(todas.Where(c => c.CategoriaId != id), "CategoriaId", "Nombre", categoria.CategoriaPadreId);

            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Categoria categoria)
        {
            try
            {
                await _categoriaService.EditarCategoriaAsync(categoria.CategoriaId, categoria.Nombre);

                await _categoriaService.CambiarCategoriaPadre(categoria.CategoriaId, categoria.CategoriaPadreId);

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var todas = await _categoriaService.ObtenerCategoriasAsync();
                ViewBag.Categorias = new SelectList(todas.Where(c => c.CategoriaId != categoria.CategoriaId), "CategoriaId", "Nombre");
                return View(categoria);
            }
        }

        // ELIMINAR
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            bool eliminado = await _categoriaService.EliminarCategoriaAsync(id);
            if (!eliminado)
            {
                TempData["Error"] = "No se puede eliminar: la categoría tiene productos o subcategorías.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}