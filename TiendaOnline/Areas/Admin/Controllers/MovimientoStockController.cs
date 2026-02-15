using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Areas.Admin.ViewModels.MovimientoStock;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.DTOs.Admin.MovimientoStock;
using TiendaOnline.Services.IServices.Admin;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class MovimientoStockController : Controller
    {
        private readonly IMovimientoStockService _movimientoService;

        public MovimientoStockController(IMovimientoStockService movimientoService)
        {
            _movimientoService = movimientoService;
        }

        [HttpGet]
        public async Task<IActionResult> Movimientos([FromQuery] MovimientoFiltrosDto filtros)
        {
            ViewData["Title"] = "Movimientos";

            // Obtenemos los datos paginados
            var resultadoPaginado = await _movimientoService.ObtenerMovimientosPaginadosAsync(filtros);

            // Cargamos las opciones para los dropdowns (esto debería venir de tu servicio)
            var tipos = await _movimientoService.ObtenerTiposMovimientoAsync();

            var model = new MovimientosViewModel
            {
                MovimientosPaginados = resultadoPaginado,
                Filtros = filtros,
                TiposMovimiento = tipos
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Historial(int id)
        {
            var historial = await _movimientoService.ObtenerHistorialPorProductoAsync(id);
            // Podés pasar el producto en el ViewBag para el título de la página
            return View(historial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEntrada(RegistroStockDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Datos inválidos. Revisá el formulario.";
                return RedirectToAction("Details", "Productos", new { id = dto.ProductoId });
            }

            try
            {
                await _movimientoService.RegistrarEntradaAsync(dto);
                TempData["MensajeExito"] = "¡Stock incrementado con éxito!";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Movimientos", "MovimientoStock");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAjuste(AjusteManualDto dto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _movimientoService.RegistrarAjusteManualAsync(dto);
                    TempData["MensajeExito"] = "Se aplicó el ajuste de stock correctamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }

            return RedirectToAction("Movimientos", "MovimientoStock");
        }
    }
}
