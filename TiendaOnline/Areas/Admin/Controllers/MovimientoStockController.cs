using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Areas.Admin.ViewModels.MovimientoStock;
using TiendaOnline.Services.DTOs.Admin.MovimientoStock;
using TiendaOnline.Services.IServices;
using TiendaOnline.Services.IServices.Admin;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class MovimientoStockController : Controller
    {
        private readonly IMovimientoStockService _movimientoService;
        private readonly IProductoService _productoService;

        public MovimientoStockController(IMovimientoStockService movimientoService, IProductoService productoService)
        {
            _movimientoService = movimientoService;
            _productoService = productoService;
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
            ViewData["Title"] = "Historial";
            var historial = await _movimientoService.ObtenerHistorialPorProductoAsync(id);
            return View(historial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEntrada(RegistroStockDto dto)
        {
            // Validación de Servidor
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Datos de entrada inválidos. Revisá la cantidad y observaciones.";
                return RedirectToAction("Movimientos", "MovimientoStock");
            }

            try
            {
                await _movimientoService.RegistrarEntradaAsync(dto);
                TempData["MensajeExito"] = "¡Entrada de mercadería registrada!";
            }
            catch (Exception ex)
            {
                // En un entorno real, aquí iría un _logger.LogError(ex);
                TempData["MensajeError"] = "Error al procesar la entrada: " + ex.Message;
            }

            return RedirectToAction("Movimientos", "MovimientoStock");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAjuste(AjusteManualDto dto)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["MensajeError"] = error ?? "Datos de ajuste inválidos.";
                return RedirectToAction("Movimientos", "MovimientoStock");
            }

            try
            {
                await _movimientoService.RegistrarAjusteManualAsync(dto);
                TempData["MensajeExito"] = "Ajuste de inventario aplicado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "No se pudo aplicar el ajuste: " + ex.Message;
            }

            return RedirectToAction("Movimientos", "MovimientoStock");
        }
    }
}
