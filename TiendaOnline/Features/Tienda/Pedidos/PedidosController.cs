using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;

namespace TiendaOnline.Features.Tienda.Pedidos
{
    [Route("[controller]")]
    public class PedidosController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IPedidoCommandService _pedidoCommandService;

        public PedidosController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> MisPedidos()
        {
            ViewData["Title"] = "Mis Pedidos";
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                return Unauthorized();
            }

            int usuarioId = int.Parse(claim.Value);

            var pedidos = await _pedidoQueryService.ObtenerPedidosDeUsuarioAsync(usuarioId);

            return View(pedidos);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Detalles(int id)
        {
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(id);
            if (pedido == null)
                return NotFound();

            ViewData["Title"] = $"Pedido #{pedido.PedidoId.ToString("D6")}";
            return View(pedido);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarCompra()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                return Unauthorized();

            int usuarioId = int.Parse(claim.Value);

            var resultado = await _pedidoCommandService.CrearPedidoAsync(usuarioId);

            if (resultado < 0)
            {
                TempData["MensajeError"] = "No se ha podido crear el pedido.";
                return RedirectToAction("Index", "Carrito");
            }

            TempData["MensajeExito"] = "¡Pedido realizado con éxito!";
            return RedirectToAction("Detalles", new { id = resultado });
        }
    }
}
