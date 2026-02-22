using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TiendaOnline.Features.Admin.MovimientosStock
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class MovimientosStockController : Controller
    {
        private readonly IMovimientoStockService _movimientoService;

        public MovimientosStockController(IMovimientoStockService movimientoService)
        {
            _movimientoService = movimientoService;
        }

        [HttpGet("[action]")]
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

        [HttpGet("[action]")]
        public async Task<IActionResult> Historial(int id)
        {
            ViewData["Title"] = "Historial";
            var historial = await _movimientoService.ObtenerHistorialPorProductoAsync(id);
            return View(historial);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEntrada(RegistroStockDto dto)
        {
            // Validación de Servidor
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Datos de entrada inválidos. Revisá la cantidad y observaciones.";
                return RedirectToAction(nameof(Movimientos));
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

            return RedirectToAction(nameof(Movimientos));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAjuste(AjusteManualDto dto)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["MensajeError"] = error ?? "Datos de ajuste inválidos.";
                return RedirectToAction(nameof(Movimientos));
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

            return RedirectToAction(nameof(Movimientos));
        }
    }
}
