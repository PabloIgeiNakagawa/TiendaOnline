using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;

namespace TiendaOnline.Controllers
{
    public class PedidoController : Controller
    {
        private readonly IPedidoService _pedidoService;
        private readonly IUsuarioService _usuarioService;

        public PedidoController(IPedidoService pedidoService, IUsuarioService usuarioService)
        {
            _pedidoService = pedidoService;
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> MisPedidos()
        {
            var claim = User.FindFirst("UsuarioId");
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
        [Authorize(Roles = "Administrador, Repartidor")]
        public async Task<IActionResult> CambiarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado)
        {
            switch (nuevoEstado)
            {
                case EstadoPedido.Enviado:
                    await _pedidoService.PedidoEnviadoAsync(pedidoId);
                    break;
                case EstadoPedido.Entregado:
                    await _pedidoService.PedidoEntregadoAsync(pedidoId);
                    break;
                case EstadoPedido.Cancelado:
                    await _pedidoService.PedidoCanceladoAsync(pedidoId);
                    break;
                default:
                    TempData["MensajeError"] = "Estado de pedido no válido.";
                    return RedirectToAction("Detalles", new { id = pedidoId });
            }

            TempData["MensajeExito"] = $"Estado del pedido actualizado a {nuevoEstado}";
            return RedirectToAction("Detalles", new { id = pedidoId });
        }
    }
}
