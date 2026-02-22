using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.MovimientosStock;
using TiendaOnline.Features.Admin.Pedidos;
using TiendaOnline.Features.Tienda.Carritos;
using TiendaOnline.Services.Commons.Models;

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

        public async Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(string? busqueda, EstadoPedido? estado, DateTime? desde, DateTime? hasta, string? monto, int pagina, int cantidad)
        {
            var query = _context.Pedidos.AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(p => p.PedidoId.ToString().Contains(busqueda) ||
                                         p.Usuario.Nombre.Contains(busqueda) ||
                                         p.Usuario.Apellido.Contains(busqueda));
            }

            if (estado.HasValue) query = query.Where(p => p.Estado == estado.Value);
            if (desde.HasValue) query = query.Where(p => p.FechaPedido >= desde.Value);
            if (hasta.HasValue) query = query.Where(p => p.FechaPedido <= hasta.Value);

            // Filtro de Monto
            if (!string.IsNullOrEmpty(monto))
            {
                var qMonto = query.Select(p => new { p, Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) });
                if (monto == "bajo") query = qMonto.Where(x => x.Total < 250000).Select(x => x.p);
                if (monto == "medio") query = qMonto.Where(x => x.Total >= 250000 && x.Total <= 1000000).Select(x => x.p);
                if (monto == "alto") query = qMonto.Where(x => x.Total > 1000000).Select(x => x.p);
            }

            // Conteo Total
            var total = await query.CountAsync();

            // Paginación y Mapeo a DTO
            var items = await query
                .OrderByDescending(p => p.FechaPedido)
                .Skip((pagina - 1) * cantidad)
                .Take(cantidad)
                .Select(p => new PedidoListadoDto
                {
                    PedidoId = p.PedidoId,
                    NombreCliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    EmailCliente = p.Usuario.Email,
                    FechaPedido = p.FechaPedido,
                    FechaEntrega = p.FechaEntrega,
                    Estado = p.Estado,
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad)
                })
            .ToListAsync();

            return new PagedResult<PedidoListadoDto>(items, total, pagina, cantidad);
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
                    _movimientoStockService.GenerarMovimiento(
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
