using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Enums;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.Reportes
{
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class AdminReportesController : AdminControllerBase
    {
        private readonly IReportesService _reportesService;

        public AdminReportesController(IReportesService reportesService)
        {
            _reportesService = reportesService;
        }

        // Caparazón: cero consultas del dashboard en esta acción — la página sale
        // con sus esqueletos (data-seccion + data-esqueleto) y el cuerpo completo
        // llega en UN fetch a la acción Secciones de acá abajo. El reparto y los
        // esqueletos son maquinaria de site.js con data-* de la vista, igual que
        // en el panel de /admin.
        [HttpGet("[action]")]
        public IActionResult Dashboard()
        {
            return View();
        }

        // TODO DE GOLPE: el caparazón pide su cuerpo completo en UN fetch. La
        // respuesta es el envase _SeccionesDelDashboard con TODAS las secciones
        // rendereadas por Razor (parciales, cada una con su data-seccion);
        // site.js reparte por clave y pinta el dashboard junto. La base corre
        // UNA vez por carga (los datos viven en cache del service).
        [HttpGet("[action]")]
        public async Task<IActionResult> Secciones()
        {
            var datosDto = await _reportesService.ObtenerDatosAsync(HttpContext.RequestAborted);

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

            return PartialView("_SeccionesDelDashboard", viewModel);
        }
    }
}
