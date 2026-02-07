using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Services.DTOs;
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
                UsuarioId = usuarioId,
                FechaPedido = DateTime.Now,
                Estado = EstadoPedido.Pendiente,
                DetallesPedido = new List<DetallePedido>()
            };

            foreach (var item in carrito)
            {
                var productoDb = await _context.Productos.FindAsync(item.ProductoId);

                if (productoDb == null) throw new Exception("Producto no encontrado");

                if (productoDb.Stock < item.Cantidad)
                    throw new Exception($"Sin stock para {productoDb.Nombre}");

                productoDb.Stock -= item.Cantidad;

                pedido.DetallesPedido.Add(new DetallePedido
                {
                    ProductoId = productoDb.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = productoDb.Precio
                });
            }

            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

            return pedido.PedidoId;
        }

        public async Task PedidoEnviadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.Pendiente)
                throw new Exception("El pedido no se puede enviar porque no está en estado Pendiente (Estado actual: " + pedido.Estado + ")");

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoEntregadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.Enviado)
                throw new Exception($"No se puede entregar: el pedido aún figura como {pedido.Estado}.");

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoCanceladoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado == EstadoPedido.Entregado)
                throw new Exception("No se puede cancelar un pedido que ya ha sido entregado.");

            pedido.Estado = EstadoPedido.Cancelado;
            pedido.FechaCancelado = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}
