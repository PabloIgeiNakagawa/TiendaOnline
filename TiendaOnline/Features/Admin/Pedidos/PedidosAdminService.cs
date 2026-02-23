using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.MovimientosStock;
using TiendaOnline.Application.Common;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Features.Admin.Pedidos
{
    public class PedidosAdminService : IPedidosAdminService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockService _movimientoStockService;

        public PedidosAdminService(TiendaContext context, IMovimientoStockService movimientoStockService)
        {
            _context = context;
            _movimientoStockService = movimientoStockService;
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
