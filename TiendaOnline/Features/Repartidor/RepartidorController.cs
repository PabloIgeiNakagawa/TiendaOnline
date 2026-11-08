using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Repartidor
{
    /// <summary>
    /// La cola del reparto: los pedidos entregables del rol y el pase a Entregado.
    /// El detalle de cada pedido es la misma página completa que ve el dueño/admin
    /// vía la policy VeDetallesDelPedido. Los guardas de estado (solo entrega desde
    /// Enviado) viven en el service: una DomainException llega al
    /// DomainExceptionFilter y sale como mensaje amable en el toast.
    /// </summary>
    [Route("[controller]")]
    [Authorize(Roles = "Repartidor")]
    public class RepartidorController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IPedidoCommandService _pedidoCommandService;
        private readonly ILogger<RepartidorController> _logger;

        public RepartidorController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService, ILogger<RepartidorController> logger)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new RepartidorViewModel
            {
                Pendientes = await _pedidoQueryService.ObtenerPedidosEnviadosAsync(HttpContext.RequestAborted),
                EntregadosUltimoDia = await _pedidoQueryService.ObtenerPedidosEntregadosUltimoDiaAsync(HttpContext.RequestAborted)
            };

            return View(model);
        }

        // El id viaja en la ruta: es el único valor que identifica el pedido a entregar.
        [HttpPost("Entregar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entregar(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            await _pedidoCommandService.PedidoEntregadoAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Pedido {PedidoId} marcado Entregado por {Repartidor}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito($"Pedido #{id.ToString("D6")} marcado como Entregado");

            return RedirectToAction(nameof(Index));
        }
    }
}
