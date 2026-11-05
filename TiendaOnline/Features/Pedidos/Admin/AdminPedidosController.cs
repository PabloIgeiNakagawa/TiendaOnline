using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;
using TiendaOnline.Enums;
using TiendaOnline.Extensions;

namespace TiendaOnline.Features.Pedidos.Admin
{
    [Route("admin/pedidos")]
    [Authorize(Roles = "Administrador")]
    public class AdminPedidosController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IPedidoCommandService _pedidoCommandService;
        private readonly ILogger<AdminPedidosController> _logger;

        public AdminPedidosController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService, ILogger<AdminPedidosController> logger)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(ListadoFiltrosViewModel model)
        {
            var filtros = new PedidosFiltroDto 
            {
                Busqueda = model.Busqueda,
                EstadoId = model.EstadoId,
                Desde = model.FechaDesde,
                Hasta = model.FechaHasta,
                MontoMin = model.MontoMin,
                MontoMax = model.MontoMax,
                Pagina = model.Pagina,
                Cantidad = model.TamanoPagina
            };

            // Llamamos al service con todos los filtros
            var pagedResult = await _pedidoQueryService.ObtenerPedidosPaginadosAsync(filtros);

            // Llenamos el ViewModel para que la vista sepa qué filtros están activos
            var viewModel = new PedidoListadoViewModel
            {
                PedidosPaginados = pagedResult,
                Busqueda = model.Busqueda,
                EstadoId = model.EstadoId,
                EstadoPagoId = model.EstadoPagoId,
                FechaDesde = model.FechaDesde,
                FechaHasta = model.FechaHasta,
                MontoMin = model.MontoMin,
                MontoMax = model.MontoMax,
                EstadosPedido = ((EstadoPedidoUI?)model.EstadoId).ToSelectList(),
                EstadosPago = ((EstadoPagoUI?)model.EstadoPagoId).ToSelectList()
            };

            return View(viewModel);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoCommandService.PedidoEnviadoAsync(pedidoId);
            _logger.LogInformation("Pedido {PedidoId} marcado Enviado por {Admin}", pedidoId, User.FindFirstValue(ClaimTypes.Email));
            TempData["MensajeExito"] = "Estado del pedido actualizado a Enviado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Repartidor")]
        public async Task<IActionResult> Entregar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoCommandService.PedidoEntregadoAsync(pedidoId);
            _logger.LogInformation("Pedido {PedidoId} marcado Entregado por {Admin}", pedidoId, User.FindFirstValue(ClaimTypes.Email));
            TempData["MensajeExito"] = "Estado del pedido actualizado a Entregado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoCommandService.PedidoCanceladoAsync(pedidoId);
            _logger.LogInformation("Pedido {PedidoId} cancelado por {Admin} (con devolución de stock si estaba pagado)", pedidoId, User.FindFirstValue(ClaimTypes.Email));
            TempData["MensajeExito"] = "Estado del pedido actualizado a Cancelado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }
    }
}
