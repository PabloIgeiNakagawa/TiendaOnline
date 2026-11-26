using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.Repartidor
{
    /// <summary>
    /// La cola del reparto: los pedidos entregables del rol, la toma/liberación
    /// del pedido y el pase a Entregado. El detalle de cada pedido es la misma
    /// página completa que ve el dueño/admin vía la policy VeDetallesDelPedido.
    /// Los guardas de estado viven en los services: una DomainException llega al
    /// DomainExceptionFilter y sale como mensaje amable en el toast.
    /// </summary>
    [Route("[controller]")]
    [Authorize(Roles = "Repartidor")]
    public class RepartidorController : RepartidorControllerBase
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IEstadosPedidoService _estadosPedidoService;
        private readonly IAsignacionRepartidorService _asignacionRepartidorService;
        private readonly IUbicacionRepartidorService _ubicacionRepartidorService;
        private readonly IUserSessionService _userSessionService;
        private readonly ILogger<RepartidorController> _logger;

        public RepartidorController(
            IPedidoQueryService pedidoQueryService,
            IEstadosPedidoService estadosPedidoService,
            IAsignacionRepartidorService asignacionRepartidorService,
            IUbicacionRepartidorService ubicacionRepartidorService,
            IUserSessionService userSessionService,
            ILogger<RepartidorController> logger)
        {
            _pedidoQueryService = pedidoQueryService;
            _estadosPedidoService = estadosPedidoService;
            _asignacionRepartidorService = asignacionRepartidorService;
            _ubicacionRepartidorService = ubicacionRepartidorService;
            _userSessionService = userSessionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarioId = _userSessionService.GetUserId() ?? 0;
            var pendientes = await _pedidoQueryService.ObtenerPedidosEnviadosAsync(HttpContext.RequestAborted);
            var entregadosUltimoDia = await _pedidoQueryService.ObtenerPedidosEntregadosUltimoDiaAsync(HttpContext.RequestAborted);

            var model = new RepartidorViewModel
            {
                UsuarioActualId = usuarioId,
                CompartiendoUbicacion = await _ubicacionRepartidorService.EstaCompartiendoAsync(HttpContext.RequestAborted),
                Pendientes = pendientes.Select(p => MapearFila(p, usuarioId)).ToList(),
                EntregadosUltimoDia = entregadosUltimoDia.Select(p => MapearFila(p, usuarioId)).ToList(),
                EntregadasHoy = entregadosUltimoDia.Count(p => p.FechaEntrega?.Date == DateTime.Today)
            };

            return View(model);
        }

        private static RepartidorFilaViewModel MapearFila(PedidoRepartoDto p, int usuarioActualId) => new()
        {
            PedidoId = p.PedidoId,
            NombreCliente = p.NombreCliente,
            Telefono = p.Telefono,
            DireccionCompleta = p.DireccionCompleta,
            Observaciones = p.Observaciones,
            FechaPedido = p.FechaPedido,
            FechaEnvio = p.FechaEnvio,
            FechaEntrega = p.FechaEntrega,
            Productos = p.Productos,
            RepartidorId = p.RepartidorId,
            RepartidorNombre = p.RepartidorNombre,
            AsignadoAMi = p.RepartidorId == usuarioActualId
        };

        [HttpPost("Tomar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tomar(int id)
        {
            if (id <= 0) return NotFound();

            await _asignacionRepartidorService.TomarPedidoAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Pedido {PedidoId} tomado por {Repartidor}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito($"Pedido #{id.ToString("D6")} asignado a vos");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Liberar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Liberar(int id)
        {
            if (id <= 0) return NotFound();

            await _asignacionRepartidorService.LiberarPedidoAsync(id, HttpContext.RequestAborted);
            TempData.SetMensajeExito($"Pedido #{id.ToString("D6")} devuelto a la cola");

            return RedirectToAction(nameof(Index));
        }

        // El id viaja en la ruta: es el único valor que identifica el pedido a entregar.
        [HttpPost("Entregar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entregar(int id)
        {
            if (id <= 0) return NotFound();

            var repartidorId = _userSessionService.GetUserId() ?? 0;
            await _estadosPedidoService.PedidoEntregadoPorRepartidorAsync(id, repartidorId, HttpContext.RequestAborted);
            _logger.LogInformation("Pedido {PedidoId} marcado Entregado por {Repartidor}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito($"Pedido #{id.ToString("D6")} marcado como Entregado");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Latido del GPS del teléfono. Es un fetch: el guard de coordenadas
        /// devuelve 400 con mensaje en vez de un redirect del filtro global.
        /// </summary>
        [HttpPost("Ubicacion")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("ubicacion")]
        public async Task<IActionResult> Ubicacion([FromBody] UbicacionRequest request)
        {
            try
            {
                await _ubicacionRepartidorService.RegistrarAsync(
                    request.Latitud, request.Longitud, request.PrecisionMetros, HttpContext.RequestAborted);

                return Ok(new { ok = true });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("DetenerUbicacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetenerUbicacion()
        {
            await _ubicacionRepartidorService.DetenerAsync(HttpContext.RequestAborted);
            TempData.SetMensajeExito("Dejaste de compartir tu ubicación");

            return Ok(new { ok = true });
        }
    }

    public record UbicacionRequest(double Latitud, double Longitud, double? PrecisionMetros);
}
