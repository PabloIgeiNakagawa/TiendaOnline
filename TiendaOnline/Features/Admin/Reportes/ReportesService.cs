using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Features.Admin.Reportes
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
                PedidosRecientes = await ObtenerPedidosRecientesAsync(10)
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
                .Where(dp => dp.Pedido.Estado != EstadoPedido.Cancelado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario);

            var ventasMesActual = await _context.DetallesPedido
                .Where(dp => dp.Pedido.FechaPedido >= inicioMesActual
                          && dp.Pedido.FechaPedido <= finMesActual
                          && dp.Pedido.Estado != EstadoPedido.Cancelado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario);

            var ventasMesAnterior = await _context.DetallesPedido
                .Where(dp => dp.Pedido.FechaPedido >= inicioMesAnterior
                          && dp.Pedido.FechaPedido <= finMesAnterior
                          && dp.Pedido.Estado != EstadoPedido.Cancelado)
                .SumAsync(dp => dp.Cantidad * dp.PrecioUnitario);

            var totalPedidos = await _context.Pedidos.CountAsync();
            var pedidosMesActual = await _context.Pedidos
                .Where(p => p.FechaPedido >= inicioMesActual && p.FechaPedido <= finMesActual)
                .CountAsync();
            var pedidosMesAnterior = await _context.Pedidos
                .Where(p => p.FechaPedido >= inicioMesAnterior && p.FechaPedido <= finMesAnterior)
                .CountAsync();

            var totalClientes = await _context.Usuarios.Where(u => u.Rol == 0 && u.Activo).CountAsync();
            var clientesMesActual = await _context.Usuarios
                .Where(u => u.Rol == 0 && u.Activo
                         && u.FechaCreacion >= inicioMesActual
                         && u.FechaCreacion <= finMesActual)
                .CountAsync();
            var clientesMesAnterior = await _context.Usuarios
                .Where(u => u.Rol == 0 && u.Activo
                         && u.FechaCreacion >= inicioMesAnterior
                         && u.FechaCreacion <= finMesAnterior)
                .CountAsync();

            var productosBajoStock = await _context.Productos
                .Where(p => p.Stock < 10 && p.Activo)
                .CountAsync();

            var promedioVentaPorPedido = totalPedidos > 0 ? ventasTotales / totalPedidos : 0;

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
                PorcentajeCambioVentas = CalcularPorcentajeCambio(ventasMesAnterior, ventasMesActual),
                PorcentajeCambioPedidos = CalcularPorcentajeCambio(pedidosMesAnterior, pedidosMesActual),
                PorcentajeCambioClientes = CalcularPorcentajeCambio(clientesMesAnterior, clientesMesActual)
            };
        }

        private async Task<List<ProductoMasVendidoDto>> ObtenerTopProductosAsync(int cantidad)
        {
            return await _context.DetallesPedido
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
            var totalPedidos = await _context.Pedidos.CountAsync();
            var pendientes = await _context.Pedidos.CountAsync(p => p.Estado == EstadoPedido.Pendiente);
            var enviados = await _context.Pedidos.CountAsync(p => p.Estado == EstadoPedido.Enviado);
            var entregados = await _context.Pedidos.CountAsync(p => p.Estado == EstadoPedido.Entregado);
            var cancelados = await _context.Pedidos.CountAsync(p => p.Estado == EstadoPedido.Cancelado);

            return new EstadisticasPedidosDto
            {
                TotalPendientes = pendientes,
                TotalEnviados = enviados,
                TotalEntregados = entregados,
                TotalCancelados = cancelados,
                PorcentajePendientes = totalPedidos > 0 ? (decimal)pendientes / totalPedidos * 100 : 0,
                PorcentajeEnviados = totalPedidos > 0 ? (decimal)enviados / totalPedidos * 100 : 0,
                PorcentajeEntregados = totalPedidos > 0 ? (decimal)entregados / totalPedidos * 100 : 0,
                PorcentajeCancelados = totalPedidos > 0 ? (decimal)cancelados / totalPedidos * 100 : 0
            };
        }

        private async Task<List<ProductoBajoStockDto>> ObtenerProductosBajoStockAsync(int cantidad)
        {
            var fechaInicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            return await _context.Productos
                .Where(p => p.Stock < 10 && p.Activo)
                .Select(p => new ProductoBajoStockDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria.Nombre,
                    Stock = p.Stock,
                    ImagenUrl = p.ImagenUrl,
                    CantidadVendidaUltimoMes = _context.DetallesPedido
                        .Where(dp => dp.ProductoId == p.ProductoId
                                  && dp.Pedido.FechaPedido >= fechaInicioMes
                                  && dp.Pedido.Estado != EstadoPedido.Cancelado)
                        .Sum(dp => (int?)dp.Cantidad) ?? 0
                })
                .OrderBy(p => p.Stock)
                .Take(cantidad)
                .ToListAsync();
        }

        private async Task<List<PedidoRecienteDto>> ObtenerPedidosRecientesAsync(int cantidad)
        {
            return await _context.Pedidos
                .OrderByDescending(p => p.FechaPedido)
                .Take(cantidad)
                .Select(p => new PedidoRecienteDto
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    Cliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    Total = p.DetallesPedido.Sum(dp => dp.Cantidad * dp.PrecioUnitario),
                    Estado = p.Estado == EstadoPedido.Pendiente ? "Pendiente" :
                             p.Estado == EstadoPedido.Enviado ? "Enviado" :
                             p.Estado == EstadoPedido.Entregado ? "Entregado" :
                             p.Estado == EstadoPedido.Cancelado ? "Cancelado" : "Desconocido",
                    EstadoNumero = (int)p.Estado
                })
                .ToListAsync();
        }

        private decimal CalcularPorcentajeCambio(decimal valorAnterior, decimal valorActual)
        {
            if (valorAnterior == 0) return valorActual > 0 ? 100 : 0;
            return (valorActual - valorAnterior) / valorAnterior * 100;
        }

        private string ObtenerNombreMes(int mes, int anio)
        {
            var cultura = new CultureInfo("es-ES");
            return cultura.DateTimeFormat.GetMonthName(mes) + " " + anio;
        }
    }
}


