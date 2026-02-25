using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.MovimientosStock.Queries;

namespace TiendaOnline.Features.Admin.MovimientosStock
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class MovimientosStockController : Controller
    {
        private readonly IMovimientoStockQueryService _movimientoStockQueryService;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;

        public MovimientosStockController(IMovimientoStockQueryService movimientoStockQueryService, IMovimientoStockCommandService movimientoStockCommandService)
        {
            _movimientoStockQueryService = movimientoStockQueryService;
            _movimientoStockCommandService = movimientoStockCommandService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Movimientos([FromQuery] MovimientosFiltroViewModel filtros)
        {
            ViewData["Title"] = "Movimientos";

            var dto = new MovimientoFiltrosDto
            {
                Busqueda = filtros.Busqueda,
                TipoMovimientoId = filtros.TipoMovimientoId,
                Desde = filtros.Desde,
                Hasta = filtros.Hasta,
                Pagina = filtros.Pagina,
                RegistrosPorPagina = filtros.RegistrosPorPagina
            };

            var resultadoPaginado = await _movimientoStockQueryService.ObtenerMovimientosPaginadosAsync(dto);

            var tipos = await _movimientoStockQueryService.ObtenerTiposMovimientoAsync();

            var model = new MovimientosViewModel
            {
                MovimientosPaginados = resultadoPaginado,
                Busqueda = filtros.Busqueda,
                TipoMovimientoId = filtros.TipoMovimientoId,
                Desde = filtros.Desde,
                Hasta = filtros.Hasta,
                Pagina = filtros.Pagina,
                RegistrosPorPagina = filtros.RegistrosPorPagina,
                TiposMovimiento = tipos
            };

            return View(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Historial(int id)
        {
            ViewData["Title"] = "Historial";
            var historial = await _movimientoStockQueryService.ObtenerHistorialPorProductoAsync(id);
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
                await _movimientoStockCommandService.RegistrarEntradaAsync(dto);
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
                await _movimientoStockCommandService.RegistrarAjusteManualAsync(dto);
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
