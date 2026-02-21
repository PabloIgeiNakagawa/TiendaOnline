using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Helpers;
using TiendaOnline.Services.DTOs;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Features.Tienda.Pedidos
{
    public class PedidosController : Controller
    {
        private readonly IPedidoService _pedidoService;

        public PedidosController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        public async Task<IActionResult> MisPedidos()
        {
            ViewData["Title"] = "Mis Pedidos";
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

            ViewData["Title"] = $"Pedido #{pedido.PedidoId.ToString("D6")}";
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarCompra()
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");

            if (carrito == null || carrito.Count == 0)
            {
                TempData["MensajeError"] = "El carrito está vacío.";
                return RedirectToAction("Index", "Carrito");
            }

            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                return Unauthorized();
            }
            int usuarioId = int.Parse(claim.Value);

            int pedidoId = await _pedidoService.CrearPedidoAsync(carrito, usuarioId);

            HttpContext.Session.Remove("Carrito");
            TempData["MensajeExito"] = "¡Pedido realizado con éxito!";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }
    }
}
