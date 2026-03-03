using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.Query;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class PedidoQueryService : IPedidoQueryService
    {
        private readonly TiendaContext _context;

        public PedidoQueryService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<List<PedidoListadoUsuarioDto>> ObtenerPedidosDeUsuarioAsync(int id)
        {
            return await _context.Pedidos
                .Where(p => p.UsuarioId == id)
                .Select(p => new PedidoListadoUsuarioDto
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    FechaEnvio = p.FechaEnvio,
                    FechaEntrega = p.FechaEntrega,
                    FechaCancelado = p.FechaCancelado,
                    Estado = p.Estado,

                    Productos = p.DetallesPedido
                                .Select(d => d.Producto.Nombre)
                                .ToList()
                })
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
    }
}
