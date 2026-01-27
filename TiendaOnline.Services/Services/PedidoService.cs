using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.DTOs;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Services.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly TiendaContext _context;

        public PedidoService(TiendaContext context)
        {
            _context = context;
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

            await _context.SaveChangesAsync();

            return pedido.PedidoId;
        }

        public async Task PedidoEnviadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            var estadoAnterior = new { pedido.Estado, pedido.FechaEnvio };

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoEntregadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            var estadoAnterior = new { pedido.Estado, pedido.FechaEntrega };

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoCanceladoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            var estadoAnterior = new { pedido.Estado, pedido.FechaCancelado };

            pedido.Estado = EstadoPedido.Cancelado;
            pedido.FechaCancelado = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}
