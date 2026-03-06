using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.Query;
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
                .AsNoTracking()
                .Where(p => p.UsuarioId == id)
                .Select(p => new PedidoListadoUsuarioDto
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    FechaEnvio = p.FechaEnvio,
                    FechaEntrega = p.FechaEntrega,
                    FechaCancelado = p.FechaCancelado,
                    EstadoId = (int)p.Estado,
                    EstadoNombre = p.Estado.ToString(),

                    Productos = p.DetallesPedido
                                .Select(d => d.Producto.Nombre)
                                .ToList()
                })
                .ToListAsync();
        }

        public async Task<PedidoDetallesDto?> ObtenerPedidoConDetallesAsync(int id)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == id)
                .Select(p => new PedidoDetallesDto
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    FechaEnvio = p.FechaEnvio,
                    FechaEntrega = p.FechaEntrega,
                    FechaCancelado = p.FechaCancelado,
                    EstadoId = (int)p.Estado,
                    EstadoNombre = p.Estado.ToString(),

                    UsuarioId = p.Usuario.UsuarioId,
                    UsuarioNombre = p.Usuario.Nombre,
                    UsuarioEmail = p.Usuario.Email,
                    UsuarioTelefono = p.Usuario.Telefono,

                    Items = p.DetallesPedido.Select(d => new PedidoItemDto
                    {
                        ProductoNombre = d.Producto.Nombre,
                        ProductoImagenUrl = d.Producto.ImagenUrl,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(PedidosFiltroDto filtros)
        {
            var query = _context.Pedidos
                .AsNoTracking()
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(filtros.Busqueda))
            {
                query = query.Where(p => p.PedidoId.ToString().Contains(filtros.Busqueda) ||
                                         p.Usuario.Nombre.Contains(filtros.Busqueda) ||
                                         p.Usuario.Apellido.Contains(filtros.Busqueda));
            }

            if (filtros.EstadoId.HasValue) query = query.Where(p => ((int)p.Estado) == filtros.EstadoId);
            if (filtros.Desde.HasValue) query = query.Where(p => p.FechaPedido >= filtros.Desde.Value);
            if (filtros.Hasta.HasValue) query = query.Where(p => p.FechaPedido <= filtros.Hasta.Value);

            // Filtro de Monto
            if (!string.IsNullOrEmpty(filtros.Monto))
            {
                var qMonto = query.Select(p => new { p, Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) });
                if (filtros.Monto == "bajo") query = qMonto.Where(x => x.Total < 250000).Select(x => x.p);
                if (filtros.Monto == "medio") query = qMonto.Where(x => x.Total >= 250000 && x.Total <= 1000000).Select(x => x.p);
                if (filtros.Monto == "alto") query = qMonto.Where(x => x.Total > 1000000).Select(x => x.p);
            }

            // Conteo Total
            var total = await query.CountAsync();

            // Paginación y Mapeo a DTO
            var items = await query
                .OrderByDescending(p => p.FechaPedido)
                .Skip((filtros.Pagina - 1) * filtros.Cantidad)
                .Take(filtros.Cantidad)
                .Select(p => new PedidoListadoDto
                {
                    PedidoId = p.PedidoId,
                    NombreCliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    EmailCliente = p.Usuario.Email,
                    FechaPedido = p.FechaPedido,
                    FechaEntrega = p.FechaEntrega,
                    EstadoId = (int)p.Estado,
                    EstadoNombre = p.Estado.ToString(),
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad)
                })
            .ToListAsync();

            return new PagedResult<PedidoListadoDto>(items, total, filtros.Pagina, filtros.Cantidad);
        }
    }
}
