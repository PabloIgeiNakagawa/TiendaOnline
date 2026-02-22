using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Tienda.Pedidos;

namespace TiendaOnline.Features.Admin.Pedidos
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class PedidosController : Controller
    {
        private readonly IPedidosAdminService _pedidosAdminService;

        public PedidosController(IPedidosAdminService pedidosAdminService)
        {
            _pedidosAdminService = pedidosAdminService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(string? busqueda, EstadoPedido? estado, DateTime? fechaDesde, DateTime? fechaHasta, string? filtroMonto, int pagina = 1, int tamanoPagina = 10)
        {
            ViewData["Title"] = "Gestión de Pedidos";
            // Llamamos al service con todos los filtros
            var pagedResult = await _pedidosAdminService.ObtenerPedidosPaginadosAsync(
                busqueda?.Trim(),
                estado,
                fechaDesde,
                fechaHasta,
                filtroMonto,
                pagina,
                tamanoPagina
            );

            // Llenamos el ViewModel para que la vista sepa qué filtros están activos
            var viewModel = new PedidoListadoViewModel
            {
                PedidosPaginados = pagedResult,
                Busqueda = busqueda,
                Estado = estado,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                FiltroMonto = filtroMonto
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
            await _pedidosAdminService.PedidoEnviadoAsync(pedidoId);
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
            await _pedidosAdminService.PedidoEntregadoAsync(pedidoId);
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
            await _pedidosAdminService.PedidoCanceladoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Cancelado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }
    }
}
