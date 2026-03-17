using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;

namespace TiendaOnline.Features.Pedidos.Admin
{
    [Route("admin/pedidos")]
    [Authorize(Roles = "Administrador")]
    public class AdminPedidosController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IPedidoCommandService _pedidoCommandService;

        public AdminPedidosController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
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
                Monto = model.FiltroMonto,
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
                FechaDesde = model.FechaDesde,
                FechaHasta = model.FechaHasta,
                FiltroMonto = model.FiltroMonto
            };

            return View(viewModel);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Enviar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoCommandService.PedidoEnviadoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Enviado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "Administrador, Repartidor")]
        public async Task<IActionResult> Entregar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoCommandService.PedidoEntregadoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Entregado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Cancelar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoCommandService.PedidoCanceladoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Cancelado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }
    }
}
