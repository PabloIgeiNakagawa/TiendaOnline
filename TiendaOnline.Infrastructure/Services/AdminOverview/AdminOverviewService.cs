using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.AdminOverview
{
    public class AdminOverviewService : IAdminOverviewService
    {
        private readonly TiendaContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminOverviewService>? _logger;

        private const string ClaveCacheHome = "admin:overview";
        private static readonly TimeSpan VigenciaDelHome = TimeSpan.FromSeconds(30);

        // Cache y logger opcionales: el DI los inyecta; los tests crean el service
        // directo y quedan deterministas (cada instancia trae su propio cache).
        public AdminOverviewService(TiendaContext context, IMemoryCache? cache = null, ILogger<AdminOverviewService>? logger = null)
        {
            _context = context;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
            _logger = logger;
        }

        public async Task<AdminOverviewDto> ObtenerResumenHomeAsync(CancellationToken ct = default)
        {
            // El panel re-corre ~10 consultas en cada visita y su contenido no
            // exige frescura al segundo: 30 segundos de cache hacen que navegar
            // entre pantallas y volver no repagine todo (el paginado del listado
            // de atención requerida consulta directo, sin esta cache).
            return await _cache.GetOrCreateAsync(ClaveCacheHome, async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = VigenciaDelHome;
                return await ArmarResumenHomeAsync(ct);
            }) ?? new AdminOverviewDto();
        }

        private async Task<AdminOverviewDto> ArmarResumenHomeAsync(CancellationToken ct)
        {
            var dto = new AdminOverviewDto
            {
                DbOnline = await VerificarEstadoBaseDatosAsync(ct),
                AppVersion = ObtenerVersionApp()
            };

            // Caída de base: el panel se degrada amable (badge offline + secciones
            // vacías) en vez de un 500 genérico.
            if (!dto.DbOnline)
            {
                _logger?.LogWarning("Panel admin: base no accesible; se sirven secciones vacías.");
                return dto;
            }

            try
            {
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
                    TipoMovimientoId = (int)m.Tipo,
                    Fecha = m.Fecha,
                    Observaciones = m.Observaciones ?? string.Empty
                }).ToList();

                // Resumen Diario (hoy vs ayer): la plata es la cobrada, subtotal +
                // envío — misma regla de venta que el dashboard de reportes.
                var hoy = DateTime.Today;
                var manana = hoy.AddDays(1);
                var ayer = hoy.AddDays(-1);

                // Dinero + pedidos de hoy y ayer en UNA consulta: el subtotal lo
                // suma la base (antes bajaba pedidos completos con tracking para
                // sumar por mano).
                var resumenDinero = await _context.Pedidos
                    .AsNoTracking()
                    .Where(p => p.EstadoPago == EstadoPago.Aprobado && p.FechaPedido >= ayer && p.FechaPedido < manana)
                    .Select(p => new
                    {
                        DeHoy = p.FechaPedido >= hoy,
                        Envio = p.CostoEnvio,
                        Subtotal = p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario)
                    })
                    .ToListAsync(ct);

                var ventasHoy = resumenDinero.Where(v => v.DeHoy).Sum(v => v.Subtotal + v.Envio);
                var ventasAyer = resumenDinero.Where(v => !v.DeHoy).Sum(v => v.Subtotal + v.Envio);
                var pedidosHoy = resumenDinero.Count(v => v.DeHoy);
                var pedidosAyer = resumenDinero.Count - pedidosHoy;

                // Enviados de hoy y ayer en una consulta liviana (solo fechas).
                var fechasEnvio = await _context.Pedidos
                    .AsNoTracking()
                    .Where(p => p.Estado == EstadoPedido.Enviado && p.FechaEnvio >= ayer && p.FechaEnvio < manana)
                    .Select(p => p.FechaEnvio!.Value)
                    .ToListAsync(ct);
                var enviadosHoy = fechasEnvio.Count(f => f >= hoy);
                var enviadosAyer = fechasEnvio.Count - enviadosHoy;

                // Stock bajo: UNA consulta alimenta el número del resumen y el listado.
                var bajoStock = await _context.Productos
                    .AsNoTracking()
                    .Where(p => p.Activo && p.Stock < 5)
                    .OrderBy(p => p.Stock)
                    .Select(p => new ProductoBajoStockDto
                    {
                        ProductoId = p.ProductoId,
                        Nombre = p.Nombre,
                        ImagenUrl = p.ImagenUrl,
                        Categoria = p.Categoria.Nombre,
                        Stock = p.Stock
                    })
                    .ToListAsync(ct);

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

                    StockBajo = bajoStock.Count
                };

                dto.ProductosBajoStock = bajoStock.Take(5).ToList();

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
                    Total = p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario) + p.CostoEnvio,
                    EstadoPedido = p.Estado.ToString(),
                    EstadoPedidoId = (int)p.Estado
                }).ToList();
            }
            catch (Exception ex)
            {
                // Falla transitoria a mitad del armado: mismo amable degrade que
                // la caída de base (secciones calculadas quedan; el resto vacías).
                _logger?.LogWarning(ex, "Panel admin: falló el armado del resumen; se sirven las secciones ya calculadas.");
            }

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
