using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class PedidoController : Controller
    {
        private readonly IPedidoService _pedidoService;

        public PedidoController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pedidos = await _pedidoService.ObtenerPedidosConDetallesAsync();
            return View(pedidos);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Enviar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoService.PedidoEnviadoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Enviado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Repartidor")]
        public async Task<IActionResult> Entregar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoService.PedidoEntregadoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Entregado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Cancelar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _pedidoService.PedidoCanceladoAsync(pedidoId);
            TempData["MensajeExito"] = "Estado del pedido actualizado a Cancelado";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }
    }
}
