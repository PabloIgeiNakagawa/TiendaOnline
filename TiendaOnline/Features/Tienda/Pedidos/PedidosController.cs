using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;
using TiendaOnline.Domain.Entities;

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
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                return Unauthorized();
            }

            int usuarioId = int.Parse(claim.Value);

            var pedidos = await _pedidoQueryService.ObtenerPedidosDeUsuarioAsync(usuarioId);

            var viewmodel = new MisPedidosViewModel
            {
                Pedidos = pedidos
                    .OrderByDescending(p => p.FechaPedido)
                    .Select(p => new PedidoListaViewModel
                    {
                        PedidoId = p.PedidoId,
                        FechaPedido = p.FechaPedido,
                        FechaEnvio = p.FechaEnvio,
                        FechaEntrega = p.FechaEntrega,
                        FechaCancelado = p.FechaCancelado,
                        Productos = p.Productos,
                        Estado = p.Estado.ToString(),
                        EstadoCss = ObtenerClaseEstado(p.Estado)
                    })
                    .ToList()
            };

            return View(viewmodel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Detalles(int id)
        {
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(id);
            if (pedido == null)
                return NotFound();

            var subtotal = pedido.Items.Sum(d => d.Cantidad * d.PrecioUnitario);
            var iva = subtotal * 0.19m;

            var viewModel = new PedidoDetalleViewModel
            {
                PedidoId = pedido.PedidoId,
                FechaPedido = pedido.FechaPedido,
                FechaEnvio = pedido.FechaEnvio,
                FechaEntrega = pedido.FechaEntrega,
                FechaCancelado = pedido.FechaCancelado,
                Estado = pedido.Estado.ToString(),

                UsuarioNombre = pedido.UsuarioNombre,
                UsuarioEmail = pedido.UsuarioEmail,
                UsuarioTelefono = pedido.UsuarioTelefono,

                Items = pedido.Items.Select(d => new PedidoItemViewModel
                {
                    ProductoNombre = d.ProductoNombre,
                    ProductoImagenUrl = d.ProductoImagenUrl,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList(),

                Subtotal = subtotal,
                IVA = iva,
                Total = subtotal + iva,

                NumeroSeguimiento = pedido.Estado == EstadoPedido.Enviado
                    ? $"TRK{pedido.PedidoId:D6}CO"
                    : null,

                FechaEstimadaEntrega = pedido.Estado == EstadoPedido.Enviado
                    ? pedido.FechaPedido.AddDays(3)
                    : null,

                EsAdmin = User.IsInRole("Administrador"),
                EsRepartidor = User.IsInRole("Repartidor"),
                EsPropioPedido = pedido.UsuarioId.ToString() ==
                                 User.FindFirstValue(ClaimTypes.NameIdentifier),

                PuedeCancelar = pedido.Estado == EstadoPedido.Pendiente,
                PuedeEnviar = pedido.Estado == EstadoPedido.Pendiente,
                PuedeEntregar = pedido.Estado == EstadoPedido.Enviado
            };

            return View(viewModel);
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

        private string ObtenerClaseEstado(EstadoPedido estado)
        {
            return estado switch
            {
                EstadoPedido.Pendiente => "text-bg-warning",
                EstadoPedido.Enviado => "text-bg-primary",
                EstadoPedido.Entregado => "text-bg-success",
                EstadoPedido.Cancelado => "text-bg-danger",
                _ => "text-bg-secondary"
            };
        }
    }
}
