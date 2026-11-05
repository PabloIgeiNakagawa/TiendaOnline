using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.MovimientosStock.Queries;
using TiendaOnline.Features.Shared.ViewModels;

namespace TiendaOnline.Features.MovimientosStock
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class MovimientosStockController : Controller
    {
        private readonly IMovimientoStockQueryService _movimientoStockQueryService;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;
        private readonly ILogger<MovimientosStockController> _logger;

        public MovimientosStockController(IMovimientoStockQueryService movimientoStockQueryService, IMovimientoStockCommandService movimientoStockCommandService, ILogger<MovimientosStockController> logger)
        {
            _movimientoStockQueryService = movimientoStockQueryService;
            _movimientoStockCommandService = movimientoStockCommandService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Movimientos([FromQuery] MovimientosFiltroViewModel filtros)
        {
            var dto = new MovimientoFiltrosDto
            {
                Busqueda = filtros.Busqueda,
                TipoMovimientoId = filtros.TipoMovimientoId,
                Desde = filtros.Desde,
                Hasta = filtros.Hasta,
                Pagina = filtros.Pagina,
                RegistrosPorPagina = filtros.RegistrosPorPagina
            };

            var resultadoPaginado = await _movimientoStockQueryService.ObtenerMovimientosPaginadosAsync(dto, HttpContext.RequestAborted);

            var tipos = await _movimientoStockQueryService.ObtenerTiposMovimientoAsync(HttpContext.RequestAborted);

            var model = new MovimientosViewModel
            {
                MovimientosPaginados = resultadoPaginado,
                Busqueda = filtros.Busqueda,
                TipoMovimientoId = filtros.TipoMovimientoId,
                Desde = filtros.Desde,
                Hasta = filtros.Hasta,
                Pagina = filtros.Pagina,
                RegistrosPorPagina = filtros.RegistrosPorPagina,
                TiposMovimiento = tipos,
            };

            return View(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Historial(int id)
        {
            ViewData["Title"] = "Historial";
            var historial = await _movimientoStockQueryService.ObtenerHistorialPorProductoAsync(id, HttpContext.RequestAborted);
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
                await _movimientoStockCommandService.RegistrarEntradaAsync(dto, HttpContext.RequestAborted);
                TempData["MensajeExito"] = "¡Entrada de mercadería registrada!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando entrada de stock (producto {ProductoId}, cantidad {Cantidad})", dto.ProductoId, dto.Cantidad);
                TempData["MensajeError"] = "Error al procesar la entrada. Intente nuevamente.";
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
                await _movimientoStockCommandService.RegistrarAjusteManualAsync(dto, HttpContext.RequestAborted);
                TempData["MensajeExito"] = "Ajuste de inventario aplicado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aplicando ajuste manual de stock (producto {ProductoId}, cantidad {Cantidad})", dto.ProductoId, dto.Cantidad);
                TempData["MensajeError"] = "No se pudo aplicar el ajuste. Intente nuevamente.";
            }

            return RedirectToAction(nameof(Movimientos));
        }
    }
}
