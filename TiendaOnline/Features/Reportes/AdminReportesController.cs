using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Enums;

namespace TiendaOnline.Features.Reportes
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class AdminReportesController : Controller
    {
        private readonly IReportesService _reportesService;

        public AdminReportesController(IReportesService reportesService)
        {
            _reportesService = reportesService;
        }

        [HttpGet("[action]")]
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
                    TiempoPromedioPreparacionHoras = datosDto.MetricasGenerales.TiempoPromedioPreparacionHoras,
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
                    TotalNuevos = datosDto.EstadisticasPedidos.TotalNuevos,
                    TotalEnPreparacion = datosDto.EstadisticasPedidos.TotalEnPreparacion,
                    TotalEnviados = datosDto.EstadisticasPedidos.TotalEnviados,
                    TotalEntregados = datosDto.EstadisticasPedidos.TotalEntregados,
                    TotalCancelados = datosDto.EstadisticasPedidos.TotalCancelados,
                    PorcentajeNuevos = datosDto.EstadisticasPedidos.PorcentajeNuevos,
                    PorcentajeEnPreparacion = datosDto.EstadisticasPedidos.PorcentajeEnPreparacion,
                    PorcentajeEnviados = datosDto.EstadisticasPedidos.PorcentajeEnviados,
                    PorcentajeEntregados = datosDto.EstadisticasPedidos.PorcentajeEntregados,
                    PorcentajeCancelados = datosDto.EstadisticasPedidos.PorcentajeCancelados
                },

                VentasPorMetodoDePago = datosDto.VentasPorMetodoDePago.Select(v => new VentaPorMetodoDePago
                {
                    MetodoDePago = v.MetodoDePago,
                    CantidadPedidos = v.CantidadPedidos,
                    TotalVentas = v.TotalVentas,
                    PorcentajeDelTotal = v.PorcentajeDelTotal
                }).ToList(),

                VentasPorDiaHora = datosDto.VentasPorDiaHora.Select(v => new VentasPorDiaHora
                {
                    DiaSemana = v.DiaSemana,
                    OrdenDia = v.OrdenDia,
                    Madrugada = v.Madrugada,
                    Manana = v.Manana,
                    Tarde = v.Tarde,
                    Noche = v.Noche
                }).ToList(),

                StockInmovilizado = datosDto.StockInmovilizado.Select(s => new StockInmovilizado
                {
                    ProductoId = s.ProductoId,
                    Nombre = s.Nombre,
                    Categoria = s.Categoria,
                    Stock = s.Stock,
                    Precio = s.Precio,
                    ValorInvertido = s.ValorInvertido,
                    UltimaVenta = s.UltimaVenta,
                    DiasSinVenta = s.DiasSinVenta,
                    ImagenUrl = s.ImagenUrl
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
