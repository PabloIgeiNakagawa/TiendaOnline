using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Reportes
{
    public class ReportesService : IReportesService
    {
        private readonly TiendaContext _context;
        private readonly IMemoryCache _cache;

        private const string ClaveCacheDashboard = "reportes:dashboard";
        private static readonly TimeSpan VigenciaDelDashboard = TimeSpan.FromSeconds(60);

        // El cache es opcional (el DI lo inyecta; los tests crean el service sin
        // él y quedan deterministas: cada instancia arma el suyo).
        public ReportesService(TiendaContext context, IMemoryCache? cache = null)
        {
            _context = context;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<DashboardDto> ObtenerDatosAsync(CancellationToken ct = default)
        {
            // El armado recorre TODOS los pedidos del negocio (promedio de
            // preparación, mapa calendario, stock inmovilizado...). Es la consulta
            // más cara del panel y su resultado no exige frescura al segundo: se
            // guarda 60 segundos, así cada visita de un admin curioso (F5 sobre
            // F5) deja de repaginar la base completa.
            return await _cache.GetOrCreateAsync(ClaveCacheDashboard, async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = VigenciaDelDashboard;

                var dashboard = new DashboardDto
                {
                    MetricasGenerales = await ObtenerMetricasGeneralesAsync(ct),
                    TopProductos = await ObtenerTopProductosAsync(10, ct),
                    TopClientes = await ObtenerTopClientesAsync(10, ct),
                    VentasPorCategoria = await ObtenerVentasPorCategoriaAsync(ct),
                    VentasPorMes = await ObtenerVentasPorMesAsync(12, ct),
                    EstadisticasPedidos = await ObtenerEstadisticasPedidosAsync(ct),
                    VentasPorMetodoDePago = await ObtenerVentasPorMetodoDePagoAsync(ct),
                    VentasPorDiaHora = await ObtenerVentasPorDiaHoraAsync(ct),
                    StockInmovilizado = await ObtenerStockInmovilizadoAsync(ct)
                };

                return dashboard;
            }) ?? new DashboardDto();
        }

        private async Task<MetricasGeneralesDto> ObtenerMetricasGeneralesAsync(CancellationToken ct)
        {
            var fechaActual = DateTime.Now;
            var inicioMesActual = new DateTime(fechaActual.Year, fechaActual.Month, 1);
            // Bordes exclusivos: "inicio del mes siguiente" en vez de "último
            // día del mes" — un corte <= último-día-00:00 dejaba fuera todos los
            // pedidos con horario de ese día.
            var inicioMesSiguiente = inicioMesActual.AddMonths(1);
            var inicioMesAnterior = inicioMesActual.AddMonths(-1);

            // Regla de la venta (08/11): solo pagos aprobados, y la venta es la
            // plata REAL cobrada = subtotal de ítems + costo de envío (MercadoPago
            // cobra ambos en el mismo pago; sumar solo ítems desentendería el total).
            var ventasSubtotales = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.EstadoPago == EstadoPago.Aprobado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario, ct);

            var envioTotales = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.EstadoPago == EstadoPago.Aprobado)
                .SumAsync(p => (decimal?)p.CostoEnvio, ct) ?? 0m;
            var ventasTotales = ventasSubtotales + envioTotales;

            var ventasSubtotalesMesActual = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.FechaPedido >= inicioMesActual
                          && dp.Pedido.FechaPedido < inicioMesSiguiente
                          && dp.Pedido.EstadoPago == EstadoPago.Aprobado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario, ct);

            var envioMesActual = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= inicioMesActual
                         && p.FechaPedido < inicioMesSiguiente
                         && p.EstadoPago == EstadoPago.Aprobado)
                .SumAsync(p => (decimal?)p.CostoEnvio, ct) ?? 0m;
            var ventasMesActual = ventasSubtotalesMesActual + envioMesActual;

            var ventasSubtotalesMesAnterior = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.FechaPedido >= inicioMesAnterior
                          && dp.Pedido.FechaPedido < inicioMesActual
                          && dp.Pedido.EstadoPago == EstadoPago.Aprobado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario, ct);

            var envioMesAnterior = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= inicioMesAnterior
                         && p.FechaPedido < inicioMesActual
                         && p.EstadoPago == EstadoPago.Aprobado)
                .SumAsync(p => (decimal?)p.CostoEnvio, ct) ?? 0m;
            var ventasMesAnterior = ventasSubtotalesMesAnterior + envioMesAnterior;

            var totalPedidos = await _context.Pedidos.CountAsync(ct);
            var pedidosMesActual = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= inicioMesActual && p.FechaPedido < inicioMesSiguiente)
                .CountAsync(ct);
            var pedidosMesAnterior = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= inicioMesAnterior && p.FechaPedido < inicioMesActual)
                .CountAsync(ct);

            var totalClientes = await _context.Usuarios.Where(u => u.Rol == Rol.Usuario && u.Activo).CountAsync(ct);
            var clientesMesActual = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == Rol.Usuario && u.Activo
                         && u.FechaCreacion >= inicioMesActual
                         && u.FechaCreacion < inicioMesSiguiente)
                .CountAsync(ct);
            var clientesMesAnterior = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == Rol.Usuario && u.Activo
                         && u.FechaCreacion >= inicioMesAnterior
                         && u.FechaCreacion < inicioMesActual)
                .CountAsync(ct);

            var productosBajoStock = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Stock < 10 && p.Activo)
                .CountAsync(ct);

            // El ticket promedio reparte la plata cobrada entre los pedidos QUE
            // PAGARON: incluir pendientes/rechazados en el divisor hundía el
            // número sin motivo.
            var pedidosCobrados = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.EstadoPago == EstadoPago.Aprobado)
                .CountAsync(ct);

            var promedioVentaPorPedido = pedidosCobrados > 0 ? ventasTotales / pedidosCobrados : 0;

            var tiempoPromedioPreparacionHoras = await CalcularTiempoPromedioPreparacionAsync(ct);

            return new MetricasGeneralesDto
            {
                VentasTotales = ventasTotales,
                VentasMesActual = ventasMesActual,
                TotalPedidos = totalPedidos,
                PedidosMesActual = pedidosMesActual,
                TotalClientes = totalClientes,
                ClientesMesActual = clientesMesActual,
                ProductosBajoStock = productosBajoStock,
                PromedioVentaPorPedido = promedioVentaPorPedido,
                TiempoPromedioPreparacionHoras = tiempoPromedioPreparacionHoras,
                PorcentajeCambioVentas = CalcularPorcentajeCambio(ventasMesAnterior, ventasMesActual),
                PorcentajeCambioPedidos = CalcularPorcentajeCambio(pedidosMesAnterior, pedidosMesActual),
                PorcentajeCambioClientes = CalcularPorcentajeCambio(clientesMesAnterior, clientesMesActual)
            };
        }

        private async Task<double> CalcularTiempoPromedioPreparacionAsync(CancellationToken ct)
        {
            // La base filtra lo inútil (solo pedidos con ambas fechas); el
            // promedio es en memoria porque la aritmética de TIME por servidor
            // (DateDiff de SQL Server) no existe en el proveedor SQLite de los tests.
            var conAmbasFechas = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaEnPreparacion.HasValue && p.FechaEnvio.HasValue)
                .Select(p => new { p.FechaEnPreparacion, p.FechaEnvio })
                .ToListAsync(ct);

            var horas = conAmbasFechas
                .Select(p => (p.FechaEnvio!.Value - p.FechaEnPreparacion!.Value).TotalHours)
                .Where(h => h >= 0)
                .ToList();

            return horas.Any() ? horas.Average() : 0;
        }

        private async Task<List<VentasPorDiaHoraDto>> ObtenerVentasPorDiaHoraAsync(CancellationToken ct)
        {
            // Ventana de 12 meses: el mapa de calor describe la operación actual,
            // no la historia completa (es tráfico de pedidos, no plata: por eso
            // vale el filtro de cancelados además del rango).
            var haceDoceMeses = DateTime.Now.AddMonths(-12);
            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= haceDoceMeses && p.Estado != EstadoPedido.Cancelado)
                .Select(p => new { p.FechaPedido })
                .ToListAsync(ct);

            var diasSemana = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Lunes" },
                { DayOfWeek.Tuesday, "Martes" },
                { DayOfWeek.Wednesday, "Miércoles" },
                { DayOfWeek.Thursday, "Jueves" },
                { DayOfWeek.Friday, "Viernes" },
                { DayOfWeek.Saturday, "Sábado" },
                { DayOfWeek.Sunday, "Domingo" }
            };

            var diasOrden = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };

            var datos = new Dictionary<DayOfWeek, Dictionary<string, int>>();
            foreach (var dia in diasOrden)
            {
                datos[dia] = new Dictionary<string, int>
                {
                    { "Madrugada", 0 },
                    { "Manana", 0 },
                    { "Tarde", 0 },
                    { "Noche", 0 }
                };
            }

            foreach (var pedido in pedidos)
            {
                var diaSemana = pedido.FechaPedido.DayOfWeek;
                var hora = pedido.FechaPedido.Hour;

                string franja = hora switch
                {
                    >= 0 and < 6 => "Madrugada",
                    >= 6 and < 12 => "Manana",
                    >= 12 and < 18 => "Tarde",
                    _ => "Noche"
                };

                if (datos.ContainsKey(diaSemana))
                {
                    datos[diaSemana][franja]++;
                }
            }

            return diasOrden.Select((dia, index) => new VentasPorDiaHoraDto
            {
                DiaSemana = diasSemana[dia],
                OrdenDia = index,
                Madrugada = datos[dia]["Madrugada"],
                Manana = datos[dia]["Manana"],
                Tarde = datos[dia]["Tarde"],
                Noche = datos[dia]["Noche"]
            }).ToList();
        }

        private async Task<List<StockInmovilizadoDto>> ObtenerStockInmovilizadoAsync(CancellationToken ct)
        {
            var fechaLimite = DateTime.Now.AddDays(-90);

            // El armado es in-memory: si el fetch no trae la categoría, p.Categoria
            // queda NULL en el contexto fresco del request y el Nombre explota.
            var productosConStock = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Stock > 0 && p.Activo)
                .ToListAsync(ct);

            var productosVendidosRecientemente = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.EstadoPago == EstadoPago.Aprobado
                          && dp.Pedido.FechaPedido >= fechaLimite)
                .Select(dp => dp.ProductoId)
                .Distinct()
                .ToListAsync(ct);

            var ultimaVentaTodos = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.EstadoPago == EstadoPago.Aprobado)
                .GroupBy(dp => dp.ProductoId)
                .Select(g => new { ProductoId = g.Key, UltimaVenta = g.Max(dp => dp.Pedido.FechaPedido) })
                .ToListAsync(ct);

            var ultimaVentaMap = ultimaVentaTodos.ToDictionary(p => p.ProductoId, p => p.UltimaVenta);

            var stockInmovilizado = productosConStock
                .Where(p => !productosVendidosRecientemente.Contains(p.ProductoId))
                .Select(p =>
                {
                    var ultimaVenta = ultimaVentaMap.ContainsKey(p.ProductoId) ? ultimaVentaMap[p.ProductoId] : (DateTime?)null;
                    // Sin ventas = null: la vista muestra el badge "Nunca", no
                    // un mentiroso "90 días" heredado del viejo default.
                    var diasSinVenta = ultimaVenta.HasValue
                        ? (int)(DateTime.Now - ultimaVenta.Value).Days
                        : (int?)null;

                    return new StockInmovilizadoDto
                    {
                        ProductoId = p.ProductoId,
                        Nombre = p.Nombre,
                        Categoria = p.Categoria.Nombre,
                        Stock = p.Stock,
                        Precio = p.Precio,
                        ValorInvertido = p.Stock * p.Precio,
                        ImagenUrl = p.ImagenUrl,
                        UltimaVenta = ultimaVenta,
                        DiasSinVenta = diasSinVenta
                    };
                })
                .OrderByDescending(s => s.ValorInvertido)
                .ToList();

            return stockInmovilizado;
        }

        private async Task<List<ProductoMasVendidoDto>> ObtenerTopProductosAsync(int cantidad, CancellationToken ct)
        {
            return await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.EstadoPago == EstadoPago.Aprobado)
                .GroupBy(dp => new { dp.ProductoId, dp.Producto.Nombre, dp.Producto.ImagenUrl, Categoria = dp.Producto.Categoria.Nombre })
                .Select(g => new ProductoMasVendidoDto
                {
                    ProductoId = g.Key.ProductoId,
                    Nombre = g.Key.Nombre,
                    Categoria = g.Key.Categoria,
                    ImagenUrl = g.Key.ImagenUrl,
                    CantidadVendida = g.Sum(dp => dp.Cantidad),
                    TotalVentas = g.Sum(dp => dp.Cantidad * dp.PrecioUnitario)
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(cantidad)
                .ToListAsync(ct);
        }

        private async Task<List<ClienteTopDto>> ObtenerTopClientesAsync(int cantidad, CancellationToken ct)
        {
            var clientes = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.EstadoPago == EstadoPago.Aprobado && p.Usuario.Rol == Rol.Usuario)
                .GroupBy(p => new { p.UsuarioId, p.Usuario.Nombre, p.Usuario.Apellido, p.Usuario.Email })
                .Select(g => new
                {
                    g.Key.UsuarioId,
                    NombreCompleto = g.Key.Nombre + " " + g.Key.Apellido,
                    g.Key.Email,
                    TotalPedidos = g.Count(),
                    TotalGastado = g.SelectMany(p => p.DetallesPedido).Sum(dp => dp.Cantidad * dp.PrecioUnitario) + g.Sum(p => p.CostoEnvio),
                    UltimaCompra = g.Max(p => p.FechaPedido)
                })
                .OrderByDescending(c => c.TotalGastado)
                .Take(cantidad)
                .ToListAsync(ct);

            return clientes.Select(c => new ClienteTopDto
            {
                UsuarioId = c.UsuarioId,
                NombreCompleto = c.NombreCompleto,
                Email = c.Email,
                TotalPedidos = c.TotalPedidos,
                TotalGastado = c.TotalGastado,
                UltimaCompra = c.UltimaCompra
            }).ToList();
        }

        private async Task<List<VentaPorCategoriaDto>> ObtenerVentasPorCategoriaAsync(CancellationToken ct)
        {
            var ventas = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.EstadoPago == EstadoPago.Aprobado)
                .GroupBy(dp => dp.Producto.Categoria.Nombre)
                .Select(g => new VentaPorCategoriaDto
                {
                    Categoria = g.Key,
                    CantidadProductos = g.Sum(dp => dp.Cantidad),
                    TotalVentas = g.Sum(dp => dp.Cantidad * dp.PrecioUnitario)
                })
                .OrderByDescending(v => v.TotalVentas)
                .ToListAsync(ct);

            var totalVentas = ventas.Sum(v => v.TotalVentas);
            foreach (var venta in ventas)
            {
                venta.PorcentajeDelTotal = totalVentas > 0 ? (int)(venta.TotalVentas / totalVentas * 100) : 0;
            }

            return ventas;
        }

        private async Task<List<VentaPorMesDto>> ObtenerVentasPorMesAsync(int meses, CancellationToken ct)
        {
            var fechaInicio = DateTime.Now.AddMonths(-meses);

            var ventasPorMes = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= fechaInicio && p.EstadoPago == EstadoPago.Aprobado)
                .GroupBy(p => new { p.FechaPedido.Year, p.FechaPedido.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TotalVentas = g.SelectMany(p => p.DetallesPedido).Sum(dp => dp.Cantidad * dp.PrecioUnitario) + g.Sum(p => p.CostoEnvio),
                    CantidadPedidos = g.Count()
                })
                .OrderBy(v => v.Year).ThenBy(v => v.Month)
                .ToListAsync(ct);

            return ventasPorMes.Select(v => new VentaPorMesDto
            {
                Mes = v.Month,
                Anio = v.Year,
                NombreMes = ObtenerNombreMes(v.Month, v.Year),
                TotalVentas = v.TotalVentas,
                CantidadPedidos = v.CantidadPedidos
            }).ToList();
        }

        private async Task<EstadisticasPedidosDto> ObtenerEstadisticasPedidosAsync(CancellationToken ct)
        {
            var estados = await _context.Pedidos
                .AsNoTracking()
                .GroupBy(p => p.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync(ct);

            var totalPedidos = estados.Sum(e => e.Cantidad);

            int nuevos = estados.FirstOrDefault(e => e.Estado == EstadoPedido.Nuevo)?.Cantidad ?? 0;
            int enPreparacion = estados.FirstOrDefault(e => e.Estado == EstadoPedido.EnPreparacion)?.Cantidad ?? 0;
            int enviados = estados.FirstOrDefault(e => e.Estado == EstadoPedido.Enviado)?.Cantidad ?? 0;
            int entregados = estados.FirstOrDefault(e => e.Estado == EstadoPedido.Entregado)?.Cantidad ?? 0;
            int cancelados = estados.FirstOrDefault(e => e.Estado == EstadoPedido.Cancelado)?.Cantidad ?? 0;

            return new EstadisticasPedidosDto
            {
                TotalNuevos = nuevos,
                TotalEnPreparacion = enPreparacion,
                TotalEnviados = enviados,
                TotalEntregados = entregados,
                TotalCancelados = cancelados,
                PorcentajeNuevos = totalPedidos > 0 ? (decimal)nuevos / totalPedidos * 100 : 0,
                PorcentajeEnPreparacion = totalPedidos > 0 ? (decimal)enPreparacion / totalPedidos * 100 : 0,
                PorcentajeEnviados = totalPedidos > 0 ? (decimal)enviados / totalPedidos * 100 : 0,
                PorcentajeEntregados = totalPedidos > 0 ? (decimal)entregados / totalPedidos * 100 : 0,
                PorcentajeCancelados = totalPedidos > 0 ? (decimal)cancelados / totalPedidos * 100 : 0
            };
        }

        private async Task<List<VentaPorMetodoDePagoDto>> ObtenerVentasPorMetodoDePagoAsync(CancellationToken ct)
        {
            var ventas = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.EstadoPago == EstadoPago.Aprobado)
                .GroupBy(p => p.MetodoDePago.Nombre)
                .Select(g => new VentaPorMetodoDePagoDto
                {
                    MetodoDePago = g.Key,
                    CantidadPedidos = g.Count(),
                    TotalVentas = g.SelectMany(p => p.DetallesPedido).Sum(dp => dp.Cantidad * dp.PrecioUnitario) + g.Sum(p => p.CostoEnvio)
                })
                .OrderByDescending(v => v.TotalVentas)
                .ToListAsync(ct);

            var totalVentas = ventas.Sum(v => v.TotalVentas);
            foreach (var venta in ventas)
            {
                venta.PorcentajeDelTotal = totalVentas > 0 ? (int)(venta.TotalVentas / totalVentas * 100) : 0;
            }

            return ventas;
        }

        private decimal CalcularPorcentajeCambio(decimal valorAnterior, decimal valorActual)
        {
            if (valorAnterior == 0) return valorActual > 0 ? 100 : 0;
            return (valorActual - valorAnterior) / valorAnterior * 100;
        }

        private string ObtenerNombreMes(int mes, int anio)
        {
            var cultura = new CultureInfo("es-AR");
            return cultura.DateTimeFormat.GetMonthName(mes) + " " + anio;
        }
    }
}


