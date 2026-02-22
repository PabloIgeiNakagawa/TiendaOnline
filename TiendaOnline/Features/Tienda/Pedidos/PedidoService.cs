using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.MovimientosStock;
using TiendaOnline.Features.Tienda.Carritos;

namespace TiendaOnline.Features.Tienda.Pedidos
{
    public class PedidoService : IPedidoService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockService _movimientoStockService;

        public PedidoService(TiendaContext context, IMovimientoStockService movimientoStockService)
        {
            _context = context;
            _movimientoStockService = movimientoStockService;
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
            // Iniciamos una transacción. Si algo falla, NO se resta stock ni se crea pedido.
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Traemos todos los productos necesarios en UNA sola consulta (Mejora de rendimiento)
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

                    // Restamos Stock en la tabla Producto
                    producto.Stock -= item.Cantidad;

                    // Registramos el movimiento de stock (SALIDA)
                    // Pasamos cantidad negativa porque es una salida
                    _movimientoStockService.GenerarMovimiento(
                        producto,
                        -item.Cantidad,
                        TipoMovimiento.SalidaVenta,
                        null, // Se vincula solo al agregar el pedido al contexto
                        "Venta Pedido Online"
                    );

                    // Agregamos el detalle al pedido
                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = producto.Precio
                    });
                }

                _context.Pedidos.Add(pedido);

                // Guardamos todo junto
                await _context.SaveChangesAsync();

                // Si llegamos acá, confirmamos los cambios en la DB
                await transaction.CommitAsync();

                return pedido.PedidoId;
            }
            catch (Exception)
            {
                // Si hubo error, deshacemos todo (el stock vuelve a su valor original)
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
