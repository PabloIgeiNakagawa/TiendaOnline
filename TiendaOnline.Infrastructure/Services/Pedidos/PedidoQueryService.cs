using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.DTOs;
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

        private static string? FormatearDireccionEnvio(TiendaOnline.Domain.Entities.Pedido p)
        {
            if (p.EsEnvioADomicilio == false) return null;

            return $"{p.EnvioCalle ?? ""} {p.EnvioNumero ?? ""}, Piso: {p.EnvioPiso ?? "N/A"}, Depto: {p.EnvioDepartamento ?? "N/A"}, {p.EnvioLocalidad ?? ""}, {p.EnvioProvincia ?? ""}";
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
                    EstadoPagoId = (int)p.EstadoPago,

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
                    EstadoPagoId = (int)p.EstadoPago,
                    MetodoPagoId = p.MetodoDePagoId,

                    UsuarioId = p.Usuario.UsuarioId,
                    UsuarioNombre = p.Usuario.Nombre,
                    UsuarioEmail = p.Usuario.Email,
                    UsuarioTelefono = p.Usuario.Telefono,

                    DireccionCompleta = FormatearDireccionEnvio(p),
                    Observaciones = p.EnvioObservaciones,


                    Items = p.DetallesPedido.Select(d => new PedidoItemDto
                    {
                        ProductoNombre = d.Producto.Nombre,
                        ProductoImagenUrl = d.Producto.ImagenUrl,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    }).ToList(),

                    TransaccionPagoId = p.TransaccionPagoId,
                    CostoEnvio = p.CostoEnvio
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ComprobantePedidoDto?> ObtenerComprobanteDtoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new
                {
                    p.PedidoId,
                    p.FechaPedido,
                    p.CostoEnvio,
                    UsuarioNombre = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    UsuarioEmail = p.Usuario.Email,
                    UsuarioTelefono = p.Usuario.Telefono,
                    DireccionCompleta = FormatearDireccionEnvio(p),
                    MetodoPago = p.MetodoDePago.Nombre,
                    EstadoPagoId = (int)p.EstadoPago,
                    p.TransaccionPagoId,
                    Items = p.DetallesPedido.Select(d => new
                    {
                        d.Producto.Nombre,
                        d.Cantidad,
                        d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario
                    }).ToList(),
                    Subtotal = p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario)
                })
                .FirstOrDefaultAsync();

            if (pedido == null) return null;

            var total = pedido.Subtotal + pedido.CostoEnvio;
            var numeroComprobante = $"CI-0001-{pedido.PedidoId:D6}";

            return new ComprobantePedidoDto(
                pedido.PedidoId,
                numeroComprobante,
                DateTime.Now,
                pedido.UsuarioNombre,
                pedido.UsuarioEmail,
                pedido.UsuarioTelefono ?? "N/A",
                pedido.DireccionCompleta,
                pedido.MetodoPago,
                pedido.EstadoPagoId,
                pedido.TransaccionPagoId,
                pedido.Items.Select(i => new ComprobanteItemDto(i.Nombre, i.Cantidad, i.PrecioUnitario, i.Subtotal)).ToList(),
                pedido.Subtotal,
                pedido.CostoEnvio,
                total
            );
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
            if (filtros.EstadoPagoId.HasValue) query = query.Where(p => ((int)p.EstadoPago) == filtros.EstadoPagoId);
            if (filtros.Desde.HasValue) query = query.Where(p => p.FechaPedido >= filtros.Desde.Value);
            if (filtros.Hasta.HasValue) query = query.Where(p => p.FechaPedido <= filtros.Hasta.Value);

            // Filtro de Monto
            if (filtros.MontoMin.HasValue || filtros.MontoMax.HasValue)
            {
                // Proyectamos el total una sola vez para no repetir el cálculo del Sum
                var qMonto = query.Select(p => new {
                    p,
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) + p.CostoEnvio
                });

                if (filtros.MontoMin.HasValue)
                    qMonto = qMonto.Where(x => x.Total >= filtros.MontoMin.Value);

                if (filtros.MontoMax.HasValue)
                    qMonto = qMonto.Where(x => x.Total <= filtros.MontoMax.Value);

                query = qMonto.Select(x => x.p);
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
                    NombreCliente = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                    EmailCliente = p.Usuario.Email,
                    FechaPedido = p.FechaPedido,
                    FechaEntrega = p.FechaEntrega,
                    EstadoId = (int)p.Estado,
                    EstadoPagoId = (int)p.EstadoPago,
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) + p.CostoEnvio
                })
            .ToListAsync();

            return new PagedResult<PedidoListadoDto>(items, total, filtros.Pagina, filtros.Cantidad);
        }
    }
}
