using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Domain.DTOs;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Controllers
{
    public class PedidoController : Controller
    {
        private readonly IPedidoService _pedidoService;

        public PedidoController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        public async Task<IActionResult> MisPedidos()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                return Unauthorized();
            }

            int usuarioId = int.Parse(claim.Value);

            var pedidos = await _pedidoService.ObtenerPedidosDeUsuarioAsync(usuarioId);

            return View(pedidos);
        }

        public async Task<IActionResult> Detalles(int id)
        {
            var pedido = await _pedidoService.ObtenerPedidoConDetallesAsync(id);
            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarCompra([FromForm] List<ItemCarrito> carrito)
        {
            if (carrito == null || !carrito.Any())
            {
                TempData["MensajeError"] = "El carrito está vacío.";
                return RedirectToAction("Index", "Carrito");
            }

            var claim = User.FindFirst("UsuarioId");
            if (claim == null)
            {
                return Unauthorized();
            }

            int usuarioId = int.Parse(claim.Value);

            int pedidoId = await _pedidoService.CrearPedidoAsync(carrito, usuarioId);

            TempData["MensajeExito"] = "¡Pedido realizado con éxito!";
            return RedirectToAction("Detalles", new { id = pedidoId });
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
