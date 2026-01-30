using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Areas.Admin.ViewModels.Reportes;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly IReportesService _reportesService;

        public ReportesController(IReportesService reportesService)
        {
            _reportesService = reportesService;
        }

        // Vista principal del dashboard
        public async Task<IActionResult> Dashboard()
        {
            var datosDto = await _reportesService.ObtenerDatosAsync();

            var viewModel = new DashboardViewModel
            {
                MetricasGenerales = new MetricasGenerales
                {
                    VentasTotales = datosDto.MetricasGenerales.VentasTotales,
                    VentasMesActual = datosDto.MetricasGenerales.VentasMesActual,
                    TotalPedidos = datosDto.MetricasGenerales.TotalPedidos,
                    PedidosMesActual = datosDto.MetricasGenerales.PedidosMesActual,
                    TotalClientes = datosDto.MetricasGenerales.TotalClientes,
                    ClientesMesActual = datosDto.MetricasGenerales.ClientesMesActual,
                    ProductosBajoStock = datosDto.MetricasGenerales.ProductosBajoStock,
                    PromedioVentaPorPedido = datosDto.MetricasGenerales.PromedioVentaPorPedido,
                    PorcentajeCambioVentas = datosDto.MetricasGenerales.PorcentajeCambioVentas,
                    PorcentajeCambioPedidos = datosDto.MetricasGenerales.PorcentajeCambioPedidos,
                    PorcentajeCambioClientes = datosDto.MetricasGenerales.PorcentajeCambioClientes
                },

                TopProductos = datosDto.TopProductos.Select(p => new ProductoMasVendido
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria,
                    CantidadVendida = p.CantidadVendida,
                    TotalVentas = p.TotalVentas,
                    ImagenUrl = p.ImagenUrl
                }).ToList(),

                TopClientes = datosDto.TopClientes.Select(c => new ClienteTopViewModel
                {
                    UsuarioId = c.UsuarioId,
                    NombreCompleto = c.NombreCompleto,
                    Email = c.Email,
                    TotalPedidos = c.TotalPedidos,
                    TotalGastado = c.TotalGastado,
                    UltimaCompra = c.UltimaCompra
                }).ToList(),

                VentasPorCategoria = datosDto.VentasPorCategoria.Select(v => new VentaPorCategoria
                {
                    Categoria = v.Categoria,
                    CantidadProductos = v.CantidadProductos,
                    TotalVentas = v.TotalVentas,
                    PorcentajeDelTotal = v.PorcentajeDelTotal
                }).ToList(),

                VentasPorMes = datosDto.VentasPorMes.Select(v => new VentaPorMes
                {
                    Mes = v.Mes,
                    Anio = v.Anio,
                    NombreMes = v.NombreMes,
                    TotalVentas = v.TotalVentas,
                    CantidadPedidos = v.CantidadPedidos
                }).ToList(),

                EstadisticasPedidos = new EstadisticasPedidos
                {
                    TotalPendientes = datosDto.EstadisticasPedidos.TotalPendientes,
                    TotalEnviados = datosDto.EstadisticasPedidos.TotalEnviados,
                    TotalEntregados = datosDto.EstadisticasPedidos.TotalEntregados,
                    TotalCancelados = datosDto.EstadisticasPedidos.TotalCancelados,
                    PorcentajePendientes = datosDto.EstadisticasPedidos.PorcentajePendientes,
                    PorcentajeEnviados = datosDto.EstadisticasPedidos.PorcentajeEnviados,
                    PorcentajeEntregados = datosDto.EstadisticasPedidos.PorcentajeEntregados,
                    PorcentajeCancelados = datosDto.EstadisticasPedidos.PorcentajeCancelados
                },

                ProductosBajoStock = datosDto.ProductosBajoStock.Select(p => new ProductoBajoStock
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria,
                    Stock = p.Stock,
                    CantidadVendidaUltimoMes = p.CantidadVendidaUltimoMes,
                    ImagenUrl = p.ImagenUrl
                }).ToList(),

                PedidosRecientes = datosDto.PedidosRecientes.Select(p => new PedidoReciente
                {
                    PedidoId = p.PedidoId,
                    FechaPedido = p.FechaPedido,
                    Cliente = p.Cliente,
                    Total = p.Total,
                    Estado = p.Estado,
                    EstadoNumero = p.EstadoNumero
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
