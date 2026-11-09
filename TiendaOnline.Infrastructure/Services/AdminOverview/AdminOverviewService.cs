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

            // Atención requerida: los dos únicos grupos que piden acción humana
            // (ver PedidosQuePidenAtencion). "Desde" es el momento del hecho que
            // espera: el cobro (grupo pagado) o la apertura del pedido (grupo pendiente).
            var ahora = DateTime.Now;
            dto.PedidosEstancados = await MapearPedidosQuePidenAtencionAsync(
                PedidosQuePidenAtencion(ahora), ahora, ct);

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
            var ahora = DateTime.Now;
            var query = PedidosQuePidenAtencion(ahora);

            var totalRegistros = await query.CountAsync(ct);
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanoPagina);

            var pedidos = await MapearPedidosQuePidenAtencionAsync(
                query.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina), ahora, ct);

            return new PedidosEstancadosPaginadoDto
            {
                Pedidos = pedidos,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros
            };
        }

        // La atención requerida del panel son solo dos grupos; preguntarle por
        // los "Nuevo hace 48hs" sería consultar pedidos ya cancelados por el job
        // de vencimiento (Nuevo+Pendiente muere a las 24h):
        //  - EnPreparacion hace más de 48hs: plata cobrada esperando el despacho.
        //  - Nuevo+Pendiente con más de 16hs de vida: quedan menos de 8hs antes
        //    del vencimiento automático (el techo de 24h descarta cadáveres si
        //    el job todavía no pasó).
        private IQueryable<Pedido> PedidosQuePidenAtencion(DateTime ahora)
        {
            var haceDosDias = ahora.AddHours(-48);
            var cercaDeVencer = ahora.AddHours(-16);
            var vidaMaximaDelPendiente = ahora.AddHours(-24);

            return _context.Pedidos
                .AsNoTracking()
                .Where(p =>
                    (p.Estado == EstadoPedido.EnPreparacion && p.FechaEnPreparacion <= haceDosDias)
                    || (p.Estado == EstadoPedido.Nuevo && p.EstadoPago == EstadoPago.Pendiente
                        && p.FechaPedido > vidaMaximaDelPendiente && p.FechaPedido <= cercaDeVencer))
                .OrderBy(p => p.FechaPedido);
        }

        // El cliente y el "desde" (el momento en que empezó a esperar) salen de
        // SQL; las horas se computan en memoria contra el `ahora` capturado una
        // vez, así todas las filas de la lista miden contra el mismo reloj.
        private async Task<List<PedidoEstancadoDto>> MapearPedidosQuePidenAtencionAsync(IQueryable<Pedido> query, DateTime ahora, CancellationToken ct)
        {
            var filas = await query
                .Select(p => new
                {
                    p.PedidoId,
                    ClienteNombre = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                    Desde = p.Estado == EstadoPedido.EnPreparacion && p.FechaEnPreparacion != null
                        ? p.FechaEnPreparacion.Value
                        : p.FechaPedido
                })
                .ToListAsync(ct);

            return filas.Select(f => new PedidoEstancadoDto
            {
                PedidoId = f.PedidoId,
                ClienteNombre = f.ClienteNombre,
                Fecha = f.Desde,
                HorasTranscurridas = (ahora - f.Desde).TotalHours
            }).ToList();
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
