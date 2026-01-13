using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly TiendaContext _context;
        private readonly IAuditoriaService _auditoriaService;

        public PedidoService(TiendaContext context, IAuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        public async Task<Pedido?> ObtenerPedidoAsync(int id)
        {
            return await _context.Pedidos.FindAsync(id);
        }

        public async Task<List<Pedido>> ObtenerPedidosDeUsuarioAsync(int id)
        {
            return await _context.Pedidos.Where(p => p.UsuarioId == id).ToListAsync();
        }

        public async Task<List<Pedido>> ObtenerPedidosAsync()
        {
            return await _context.Pedidos.ToListAsync();
        }

        public async Task<List<Pedido>> ObtenerPedidosConDetallesAsync()
        {
            return await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .ThenInclude(d => d.Producto)
                .ToListAsync();
        }

        public async Task<Pedido?> ObtenerPedidoConDetallesAsync(int id)
        {
            return await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == id);
        }

        public async Task<int> CrearPedidoAsync(List<ItemCarrito> carrito, int usuarioId)
        {
            var pedido = new Pedido
            {
                UsuarioId = usuarioId, // Usamos el parámetro que recibe la función
                FechaPedido = DateTime.Now,
                Estado = EstadoPedido.Pendiente,
                DetallesPedido = carrito.Select(item => new DetallePedido
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Precio
                }).ToList()
            };

            _context.Pedidos.Add(pedido);

            if (await _context.SaveChangesAsync() > 0)
            {
                await _auditoriaService.RegistrarAccionAsync("Crear Pedido", null, new
                {
                    pedido.PedidoId,
                    pedido.UsuarioId,
                    TotalItems = pedido.DetallesPedido.Count
                });

                return pedido.PedidoId;
            }

            throw new Exception("No se pudo crear el pedido.");
        }

        public async Task PedidoEnviadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            var estadoAnterior = new { pedido.Estado, pedido.FechaEnvio };

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;

            if (await _context.SaveChangesAsync() > 0)
            {
                await _auditoriaService.RegistrarAccionAsync("Enviar Pedido", estadoAnterior, new
                {
                    pedido.PedidoId,
                    pedido.Estado,
                    pedido.FechaEnvio
                });
            }
        }

        public async Task PedidoEntregadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            var estadoAnterior = new { pedido.Estado, pedido.FechaEntrega };

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;

            if (await _context.SaveChangesAsync() > 0)
            {
                await _auditoriaService.RegistrarAccionAsync("Entregar Pedido", estadoAnterior, new
                {
                    pedido.PedidoId,
                    pedido.Estado,
                    pedido.FechaEntrega
                });
            }
            else
            {
                throw new Exception("No se pudo entregar el pedido.");
            }
        }

        public async Task PedidoCanceladoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            var estadoAnterior = new { pedido.Estado, pedido.FechaCancelado };

            pedido.Estado = EstadoPedido.Cancelado;
            pedido.FechaCancelado = DateTime.Now;

            if (await _context.SaveChangesAsync() > 0)
            {
                await _auditoriaService.RegistrarAccionAsync("Cancelar Pedido", estadoAnterior, new
                {
                    pedido.PedidoId,
                    pedido.Estado,
                    pedido.FechaCancelado
                });
            }
            else
            {
                throw new Exception("No se pudo cancelar el pedido.");
            }
        }
    }
}
