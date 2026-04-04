using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Reportes
{
    public class ReportesService : IReportesService
    {
        private readonly TiendaContext _context;
        public ReportesService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<DashboardDTO> ObtenerDatosAsync()
        {
            var dashboard = new DashboardDTO
            {
                MetricasGenerales = await ObtenerMetricasGeneralesAsync(),
                TopProductos = await ObtenerTopProductosAsync(10),
                TopClientes = await ObtenerTopClientesAsync(10),
                VentasPorCategoria = await ObtenerVentasPorCategoriaAsync(),
                VentasPorMes = await ObtenerVentasPorMesAsync(12),
                EstadisticasPedidos = await ObtenerEstadisticasPedidosAsync(),
                ProductosBajoStock = await ObtenerProductosBajoStockAsync(10),
                PedidosRecientes = await ObtenerPedidosRecientesAsync(10),
                VentasPorMetodoDePago = await ObtenerVentasPorMetodoDePagoAsync(),
                VentasPorDiaHora = await ObtenerVentasPorDiaHoraAsync(),
                StockInmovilizado = await ObtenerStockInmovilizadoAsync()
            };

            return dashboard;
        }

        private async Task<MetricasGeneralesDto> ObtenerMetricasGeneralesAsync()
        {
            var fechaActual = DateTime.Now;
            var inicioMesActual = new DateTime(fechaActual.Year, fechaActual.Month, 1);
            var finMesActual = inicioMesActual.AddMonths(1).AddDays(-1);
            var inicioMesAnterior = inicioMesActual.AddMonths(-1);
            var finMesAnterior = inicioMesActual.AddDays(-1);

            var ventasTotales = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.Estado != EstadoPedido.Cancelado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario);

            var ventasMesActual = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.FechaPedido >= inicioMesActual
                          && dp.Pedido.FechaPedido <= finMesActual
                          && dp.Pedido.Estado != EstadoPedido.Cancelado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario);

            var ventasMesAnterior = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.FechaPedido >= inicioMesAnterior
                          && dp.Pedido.FechaPedido <= finMesAnterior
                          && dp.Pedido.Estado != EstadoPedido.Cancelado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario);

            var totalPedidos = await _context.Pedidos.CountAsync();
            var pedidosMesActual = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= inicioMesActual && p.FechaPedido <= finMesActual)
                .CountAsync();
            var pedidosMesAnterior = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= inicioMesAnterior && p.FechaPedido <= finMesAnterior)
                .CountAsync();

            var totalClientes = await _context.Usuarios.Where(u => u.Rol == 0 && u.Activo).CountAsync();
            var clientesMesActual = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == 0 && u.Activo
                         && u.FechaCreacion >= inicioMesActual
                         && u.FechaCreacion <= finMesActual)
                .CountAsync();
            var clientesMesAnterior = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == 0 && u.Activo
                         && u.FechaCreacion >= inicioMesAnterior
                         && u.FechaCreacion <= finMesAnterior)
                .CountAsync();

            var productosBajoStock = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Stock < 10 && p.Activo)
                .CountAsync();

            var promedioVentaPorPedido = totalPedidos > 0 ? ventasTotales / totalPedidos : 0;

            var tiempoPromedioPreparacionHoras = await CalcularTiempoPromedioPreparacionAsync();

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

        private async Task<double> CalcularTiempoPromedioPreparacionAsync()
        {
            var todosPedidos = await _context.Pedidos
                .AsNoTracking()
                .Select(p => new { p.FechaEnPreparacion, p.FechaEnvio })
                .ToListAsync();

            var conAmbasFechas = todosPedidos
                .Where(p => p.FechaEnPreparacion.HasValue && p.FechaEnvio.HasValue)
                .Select(p => (p.FechaEnvio!.Value - p.FechaEnPreparacion!.Value).TotalHours)
                .Where(h => h >= 0)
                .ToList();

            return conAmbasFechas.Any() ? conAmbasFechas.Average() : 0;
        }

        private async Task<List<VentasPorDiaHoraDto>> ObtenerVentasPorDiaHoraAsync()
        {
            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado != EstadoPedido.Cancelado)
                .Select(p => new { p.FechaPedido })
                .ToListAsync();

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

        private async Task<List<StockInmovilizadoDto>> ObtenerStockInmovilizadoAsync()
        {
            var fechaLimite = DateTime.Now.AddDays(-90);

            var productosConStock = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Stock > 0 && p.Activo)
                .ToListAsync();

            var productosVendidosRecientemente = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.Estado != EstadoPedido.Cancelado
                          && dp.Pedido.FechaPedido >= fechaLimite)
                .Select(dp => dp.ProductoId)
                .Distinct()
                .ToListAsync();

            var ultimaVentaTodos = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.Estado != EstadoPedido.Cancelado)
                .GroupBy(dp => dp.ProductoId)
                .Select(g => new { ProductoId = g.Key, UltimaVenta = g.Max(dp => dp.Pedido.FechaPedido) })
                .ToListAsync();

            var ultimaVentaMap = ultimaVentaTodos.ToDictionary(p => p.ProductoId, p => p.UltimaVenta);

            var stockInmovilizado = productosConStock
                .Where(p => !productosVendidosRecientemente.Contains(p.ProductoId))
                .Select(p =>
                {
                    var ultimaVenta = ultimaVentaMap.ContainsKey(p.ProductoId) ? ultimaVentaMap[p.ProductoId] : (DateTime?)null;
                    var diasSinVenta = ultimaVenta.HasValue
                        ? (int)(DateTime.Now - ultimaVenta.Value).Days
                        : 90;

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

        private async Task<List<ProductoMasVendidoDto>> ObtenerTopProductosAsync(int cantidad)
        {
            return await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.Estado != EstadoPedido.Cancelado)
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
                .ToListAsync();
        }

        private async Task<List<ClienteTopDto>> ObtenerTopClientesAsync(int cantidad)
        {
            var clientes = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado != EstadoPedido.Cancelado && p.Usuario.Rol == 0)
                .GroupBy(p => new { p.UsuarioId, p.Usuario.Nombre, p.Usuario.Apellido, p.Usuario.Email })
                .Select(g => new
                {
                    g.Key.UsuarioId,
                    NombreCompleto = g.Key.Nombre + " " + g.Key.Apellido,
                    g.Key.Email,
                    TotalPedidos = g.Count(),
                    TotalGastado = g.SelectMany(p => p.DetallesPedido).Sum(dp => dp.Cantidad * dp.PrecioUnitario),
                    UltimaCompra = g.Max(p => p.FechaPedido)
                })
                .OrderByDescending(c => c.TotalGastado)
                .Take(cantidad)
                .ToListAsync();

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

        private async Task<List<VentaPorCategoriaDto>> ObtenerVentasPorCategoriaAsync()
        {
            var ventas = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.Estado != EstadoPedido.Cancelado)
                .GroupBy(dp => dp.Producto.Categoria.Nombre)
                .Select(g => new VentaPorCategoriaDto
                {
                    Categoria = g.Key,
                    CantidadProductos = g.Sum(dp => dp.Cantidad),
                    TotalVentas = g.Sum(dp => dp.Cantidad * dp.PrecioUnitario)
                })
                .OrderByDescending(v => v.TotalVentas)
                .ToListAsync();

            var totalVentas = ventas.Sum(v => v.TotalVentas);
            foreach (var venta in ventas)
            {
                venta.PorcentajeDelTotal = totalVentas > 0 ? (int)(venta.TotalVentas / totalVentas * 100) : 0;
            }

            return ventas;
        }

        private async Task<List<VentaPorMesDto>> ObtenerVentasPorMesAsync(int meses)
        {
            var fechaInicio = DateTime.Now.AddMonths(-meses);

            var ventasPorMes = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= fechaInicio && p.Estado != EstadoPedido.Cancelado)
                .GroupBy(p => new { p.FechaPedido.Year, p.FechaPedido.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TotalVentas = g.SelectMany(p => p.DetallesPedido).Sum(dp => dp.Cantidad * dp.PrecioUnitario),
                    CantidadPedidos = g.Count()
                })
                .OrderBy(v => v.Year).ThenBy(v => v.Month)
                .ToListAsync();

            return ventasPorMes.Select(v => new VentaPorMesDto
            {
                Mes = v.Month,
                Anio = v.Year,
                NombreMes = ObtenerNombreMes(v.Month, v.Year),
                TotalVentas = v.TotalVentas,
                CantidadPedidos = v.CantidadPedidos
            }).ToList();
        }

        private async Task<EstadisticasPedidosDto> ObtenerEstadisticasPedidosAsync()
        {
            var estados = await _context.Pedidos
                .AsNoTracking()
                .GroupBy(p => p.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync();

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

        private async Task<List<ProductoBajoStockDto>> ObtenerProductosBajoStockAsync(int cantidad)
        {
            var fechaInicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var ventasUltimoMes = await _context.DetallesPedido
                .AsNoTracking()
                .Where(dp => dp.Pedido.FechaPedido >= fechaInicioMes
                          && dp.Pedido.Estado != EstadoPedido.Cancelado)
                .GroupBy(dp => dp.ProductoId)
                .Select(g => new
                {
                    ProductoId = g.Key,
                    Cantidad = g.Sum(dp => dp.Cantidad)
                })
                .ToDictionaryAsync(x => x.ProductoId, x => x.Cantidad);

            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Stock < 10 && p.Activo)
                .Select(p => new ProductoBajoStockDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria.Nombre,
                    Stock = p.Stock,
                    ImagenUrl = p.ImagenUrl
                })
                .ToListAsync();

            foreach (var producto in productos)
            {
                if (ventasUltimoMes.TryGetValue(producto.ProductoId, out var cantidadVendida))
                {
                    producto.CantidadVendidaUltimoMes = cantidadVendida;
                }
            }

            return productos;
        }

        private async Task<List<PedidoRecienteDto>> ObtenerPedidosRecientesAsync(int cantidad)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPedido)
                .Take(cantidad)
                .Select(p => new PedidoRecienteDto
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    Cliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    Total = p.DetallesPedido.Sum(dp => dp.Cantidad * dp.PrecioUnitario),
                    EstadoPedidoId = (int)p.Estado
                })
                .ToListAsync();
        }

        private async Task<List<VentaPorMetodoDePagoDto>> ObtenerVentasPorMetodoDePagoAsync()
        {
            var ventas = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado != EstadoPedido.Cancelado)
                .GroupBy(p => p.MetodoDePago.Nombre)
                .Select(g => new VentaPorMetodoDePagoDto
                {
                    MetodoDePago = g.Key,
                    CantidadPedidos = g.Count(),
                    TotalVentas = g.SelectMany(p => p.DetallesPedido).Sum(dp => dp.Cantidad * dp.PrecioUnitario)
                })
                .OrderByDescending(v => v.TotalVentas)
                .ToListAsync();

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


