using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.AdminOverview
{
    public class AdminOverviewService : IAdminOverviewService
    {
        private readonly TiendaContext _context;

        public AdminOverviewService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<AdminOverviewDto> ObtenerResumenHomeAsync(CancellationToken ct = default)
        {
            var dto = new AdminOverviewDto
            {
                DbOnline = await VerificarEstadoBaseDatosAsync(ct),
                AppVersion = ObtenerVersionApp()
            };

            // Timeline de Auditoría (Últimos 5 cambios de productos o precios)
            dto.UltimosCambios = await _context.Auditorias
                .AsNoTracking()
                .OrderByDescending(a => a.Fecha)
                .Take(5)
                .Select(a => new AuditoriaEntryDto
                {
                    UsuarioNombre = $"{a.Usuario.Nombre} {a.Usuario.Apellido}",
                    Accion = a.Accion,
                    Fecha = a.Fecha
                }).ToListAsync(ct);

            // Pedidos Estancados (Estado 0: Pendiente y más de 48hs)
            var limiteFecha = DateTime.Now.AddHours(-48);
            dto.PedidosEstancados = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado == 0 && p.FechaPedido <= limiteFecha)
                .Select(p => new PedidoEstancadoDto
                {
                    PedidoId = p.PedidoId,
                    ClienteNombre = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                    Fecha = p.FechaPedido,
                    HorasTranscurridas = (DateTime.Now - p.FechaPedido).TotalHours
                }).ToListAsync(ct);

            // Últimos 10 Movimientos de Stock
            var movimientos = await _context.MovimientosStock
                .AsNoTracking()
                .Include(m => m.Producto)
                .OrderByDescending(m => m.Fecha)
                .Take(10)
                .ToListAsync(ct);

            dto.UltimosMovimientosStock = movimientos.Select(m => new MovimientoStockDto
            {
                ProductoNombre = m.Producto.Nombre,
                Cantidad = m.Cantidad,
                TipoMovimientoId = (int)Enum.Parse(typeof(TipoMovimiento), m.Tipo.ToString()),
                Fecha = m.Fecha,
                Observaciones = m.Observaciones ?? string.Empty
            }).ToList();

            // Resumen Diario
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);
            var ayer = hoy.AddDays(-1);

            // Ventas hoy - calcular desde DetallePedido
            var pedidosVentasHoy = await _context.Pedidos
                .Include(p => p.DetallesPedido)
                .Where(p => p.EstadoPago == EstadoPago.Aprobado && p.FechaPedido >= hoy && p.FechaPedido < manana)
                .ToListAsync(ct);
            var ventasHoy = pedidosVentasHoy.Sum(p => p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario));

            var pedidosVentasAyer = await _context.Pedidos
                .Include(p => p.DetallesPedido)
                .Where(p => p.EstadoPago == EstadoPago.Aprobado && p.FechaPedido >= ayer && p.FechaPedido < hoy)
                .ToListAsync(ct);
            var ventasAyer = pedidosVentasAyer.Sum(p => p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario));

            // Pedidos hoy (con pago aprobado)
            var pedidosHoy = await _context.Pedidos
                .Where(p => p.EstadoPago == EstadoPago.Aprobado && p.FechaPedido >= hoy && p.FechaPedido < manana)
                .CountAsync(ct);

            var pedidosAyer = await _context.Pedidos
                .Where(p => p.EstadoPago == EstadoPago.Aprobado && p.FechaPedido >= ayer && p.FechaPedido < hoy)
                .CountAsync(ct);

            // Pedidos enviados hoy - usar FechaEnvio
            var enviadosHoy = await _context.Pedidos
                .Where(p => p.Estado == EstadoPedido.Enviado && p.FechaEnvio >= hoy && p.FechaEnvio < manana)
                .CountAsync(ct);

            var enviadosAyer = await _context.Pedidos
                .Where(p => p.Estado == EstadoPedido.Enviado && p.FechaEnvio >= ayer && p.FechaEnvio < hoy)
                .CountAsync(ct);

            // Stock bajo (productos con stock < 5)
            var stockBajo = await _context.Productos
                .Where(p => p.Activo && p.Stock < 5)
                .CountAsync(ct);

            dto.ResumenDiario = new ResumenDiarioDto
            {
                VentasHoy = ventasHoy,
                VentasAyer = ventasAyer,
                PorcentajeVentas = ventasAyer > 0 ? (double)((ventasHoy - ventasAyer) / ventasAyer) * 100 : (ventasHoy > 0 ? 100 : 0),

                PedidosHoy = pedidosHoy,
                PedidosAyer = pedidosAyer,
                PorcentajePedidos = pedidosAyer > 0 ? ((pedidosHoy - pedidosAyer) / (double)pedidosAyer) * 100 : (pedidosHoy > 0 ? 100 : 0),

                EnviadosHoy = enviadosHoy,
                EnviadosAyer = enviadosAyer,
                PorcentajeEnviados = enviadosAyer > 0 ? ((enviadosHoy - enviadosAyer) / (double)enviadosAyer) * 100 : (enviadosHoy > 0 ? 100 : 0),

                StockBajo = stockBajo
            };

            // Productos bajo stock
            dto.ProductosBajoStock = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Activo && p.Stock < 5)
                .OrderBy(p => p.Stock)
                .Take(5)
                .Select(p => new ProductoBajoStockDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    ImagenUrl = p.ImagenUrl,
                    Categoria = p.Categoria.Nombre,
                    Stock = p.Stock
                }).ToListAsync(ct);

            // Pedidos recientes
            var pedidosRecientesQuery = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .OrderByDescending(p => p.FechaPedido)
                .Take(10)
                .ToListAsync(ct);

            dto.PedidosRecientes = pedidosRecientesQuery.Select(p => new PedidoRecienteDto
            {
                PedidoId = p.PedidoId,
                FechaPedido = p.FechaPedido,
                Cliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                Total = p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario),
                EstadoPedido = p.Estado.ToString(),
                EstadoPedidoId = (int)p.Estado
            }).ToList();

            return dto;
        }

        public async Task<PedidosEstancadosPaginadoDto> ObtenerPedidosEstancadosPaginadoAsync(int pagina, int tamanoPagina = 5, CancellationToken ct = default)
        {
            var limiteFecha = DateTime.Now.AddHours(-48);
            var query = _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado == 0 && p.FechaPedido <= limiteFecha);

            var totalRegistros = await query.CountAsync(ct);
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanoPagina);

            var pedidos = await query
                .OrderBy(p => p.FechaPedido)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .Select(p => new PedidoEstancadoDto
                {
                    PedidoId = p.PedidoId,
                    ClienteNombre = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                    Fecha = p.FechaPedido,
                    HorasTranscurridas = (DateTime.Now - p.FechaPedido).TotalHours
                }).ToListAsync(ct);

            return new PedidosEstancadosPaginadoDto
            {
                Pedidos = pedidos,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros
            };
        }

        private async Task<bool> VerificarEstadoBaseDatosAsync(CancellationToken ct)
        {
            try
            {
                return await _context.Database.CanConnectAsync(ct);
            }
            catch
            {
                return false;
            }
        }

        private string ObtenerVersionApp()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }
}
