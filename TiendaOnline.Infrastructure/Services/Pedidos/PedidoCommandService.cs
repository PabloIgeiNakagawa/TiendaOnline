using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class PedidoCommandService : IPedidoCommandService
    {
        private readonly TiendaContext _context;
        private readonly ICarritoService _carritoService;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;

        public PedidoCommandService(TiendaContext context, ICarritoService carritoService, IMovimientoStockCommandService movimientoStockCommandService)
        {
            _context = context;
            _carritoService = carritoService;
            _movimientoStockCommandService = movimientoStockCommandService;
        }

        public async Task<int> CrearPedidoAsync(int usuarioId)
        {
            var carrito = await _carritoService.ObtenerAsync();
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productoIds = carrito.Select(c => c.ProductoId).ToList();
                var productosDb = await _context.Productos
                    .Where(p => productoIds.Contains(p.ProductoId))
                    .ToListAsync();

                var pedido = new Pedido
                {
                    UsuarioId = usuarioId,
                    FechaPedido = DateTime.Now,
                    Estado = EstadoPedido.Pendiente,
                    DetallesPedido = new List<DetallePedido>()
                };

                foreach (var item in carrito)
                {
                    var producto = productosDb.FirstOrDefault(p => p.ProductoId == item.ProductoId);
                    if (producto == null) throw new Exception("Producto no encontrado");

                    if (producto.Stock < item.Cantidad)
                        throw new Exception($"Sin stock para {producto.Nombre}");

                    producto.Stock -= item.Cantidad;

                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = producto.Precio
                    });
                }

                _context.Pedidos.Add(pedido);

                await _context.SaveChangesAsync();

                foreach (var item in carrito)
                {
                    var producto = productosDb.First(p => p.ProductoId == item.ProductoId);
                    producto.Stock -= item.Cantidad;

                    _movimientoStockCommandService.GenerarMovimiento(
                        producto,
                        -item.Cantidad,
                        TipoMovimiento.SalidaVenta,
                        pedido.PedidoId,
                        $"Venta Pedido Online #{pedido.PedidoId}"
                    );
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _carritoService.VaciarAsync();

                return pedido.PedidoId;
            }
            catch (Exception)
            {
                // Si hubo error, deshacemos todo (el stock vuelve a su valor original)
                await transaction.RollbackAsync();
                throw;
            }
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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Traemos el pedido con sus detalles (Importante usar .Include)
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallesPedido)
                    .ThenInclude(dp => dp.Producto) // Traemos el producto para devolverle el stock
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

                if (pedido == null) throw new Exception("Pedido no encontrado.");

                // Validaciones de negocio
                if (pedido.Estado == EstadoPedido.Cancelado)
                    throw new Exception("El pedido ya estaba cancelado.");

                if (pedido.Estado == EstadoPedido.Entregado)
                    throw new Exception("No se puede cancelar un pedido que ya ha sido entregado.");

                if (pedido.Estado == EstadoPedido.Enviado)
                    throw new Exception("El pedido ya fue enviado, debe procesarse como devolución.");

                // Devolver el stock por cada detalle
                foreach (var detalle in pedido.DetallesPedido)
                {
                    // Sumamos lo que antes restamos
                    detalle.Producto.Stock += detalle.Cantidad;

                    // Registramos el movimiento de entrada por cancelación
                    // Cantidad positiva porque entra de nuevo
                    _movimientoStockCommandService.GenerarMovimiento(
                        detalle.Producto,
                        detalle.Cantidad,
                        TipoMovimiento.CancelacionPedido,
                        pedido.PedidoId,
                        $"Devolución por cancelación de Pedido #{pedido.PedidoId}"
                    );
                }

                // Cambiamos el estado
                pedido.Estado = EstadoPedido.Cancelado;
                pedido.FechaCancelado = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
