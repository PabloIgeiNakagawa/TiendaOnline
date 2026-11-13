using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Enums;
using TiendaOnline.Extensions;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.Pedidos.Admin
{
    [Route("admin/pedidos")]
    [Authorize(Roles = "Administrador")]
    public class AdminPedidosController : AdminControllerBase
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IGestionTransferenciaService _gestionTransferenciaService;
        private readonly IEstadosPedidoService _estadosPedidoService;
        private readonly ILogger<AdminPedidosController> _logger;

        public AdminPedidosController(IPedidoQueryService pedidoQueryService, IGestionTransferenciaService gestionTransferenciaService, IEstadosPedidoService estadosPedidoService, ILogger<AdminPedidosController> logger)
        {
            _pedidoQueryService = pedidoQueryService;
            _gestionTransferenciaService = gestionTransferenciaService;
            _estadosPedidoService = estadosPedidoService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado(ListadoFiltrosViewModel model)
        {
            var filtros = new PedidosFiltroDto
            {
                Busqueda = model.Busqueda,
                EstadoId = model.EstadoId,
                EstadoPagoId = model.EstadoPagoId,
                Desde = model.FechaDesde,
                Hasta = model.FechaHasta,
                MontoMin = model.MontoMin,
                MontoMax = model.MontoMax,
                Pagina = model.Pagina,
                Cantidad = model.TamanoPagina
            };

            // Llamamos al service con todos los filtros
            var pagedResult = await _pedidoQueryService.ObtenerPedidosPaginadosAsync(filtros, HttpContext.RequestAborted);

            // Llenamos el ViewModel para que la vista sepa qué filtros están activos
            var viewModel = new PedidoListadoViewModel
            {
                PedidosPaginados = pagedResult,
                Busqueda = model.Busqueda,
                EstadoId = model.EstadoId,
                EstadoPagoId = model.EstadoPagoId,
                FechaDesde = model.FechaDesde,
                FechaHasta = model.FechaHasta,
                MontoMin = model.MontoMin,
                MontoMax = model.MontoMax,
                EstadosPedido = ((EstadoPedido?)model.EstadoId).ToSelectList(),
                EstadosPago = ((EstadoPago?)model.EstadoPagoId).ToSelectList()
            };

            return View(viewModel);
        }

        // Export CSV de los pedidos del listado: mismos filtros del form (los viaja
        // el mismo GET vía formaction del botón), sin paginación. Descarga con BOM
        // UTF-8 y separador ";" para que Excel lo abra directo con columna total.
        [HttpGet("[action]")]
        public async Task<IActionResult> Exportar(ListadoFiltrosViewModel model)
        {
            var filtros = new PedidosFiltroDto
            {
                Busqueda = model.Busqueda,
                EstadoId = model.EstadoId,
                EstadoPagoId = model.EstadoPagoId,
                Desde = model.FechaDesde,
                Hasta = model.FechaHasta,
                MontoMin = model.MontoMin,
                MontoMax = model.MontoMax,
                Pagina = 1,
                Cantidad = int.MaxValue
            };

            var pedidos = await _pedidoQueryService.ObtenerPedidosParaExportarAsync(filtros, HttpContext.RequestAborted);

            // Formatos fijos a es-AR (coma decimal, dd/MM): el CSV no depende de la
            // cultura del server — Excel argentino lo parsea como número siempre.
            var culturaCsv = new CultureInfo("es-AR");
            var sb = new StringBuilder();
            sb.AppendLine("Pedido;Fecha;Cliente;Email;Estado;Estado del pago;Método de pago;Total;Fecha de entrega;Comprobante");

            foreach (var p in pedidos)
            {
                sb.AppendLine(string.Join(';',
                    CeldaCsv(p.PedidoId.ToString()),
                    CeldaCsv(p.FechaPedido.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                    CeldaCsv(p.NombreCliente),
                    CeldaCsv(p.EmailCliente),
                    CeldaCsv(((EstadoPedido)p.EstadoId).GetDisplayName()),
                    CeldaCsv(((EstadoPago)p.EstadoPagoId).GetDisplayName()),
                    CeldaCsv(((MetodoPagoId)p.MetodoPagoId).GetDisplayName()),
                    CeldaCsv(p.Total.ToString("N2", culturaCsv)),
                    CeldaCsv(p.FechaEntrega?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? ""),
                    CeldaCsv(p.TieneComprobante ? "Sí" : "No")));
            }

            // BOM UTF-8: sin él Excel adivina ANSI y los acentos salen rotos.
            return File(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString()),
                "text/csv",
                $"pedidos-{DateTime.Now:yyyyMMdd}.csv");
        }

        // Escapado CSV: celda con separador, comillas o salto de línea va entre
        // comillas (doblando las comillas internas), el resto sale pelado.
        private static string CeldaCsv(string valor)
        {
            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            {
                return $"\"{valor.Replace("\"", "\"\"")}\"";
            }

            return valor;
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _estadosPedidoService.PedidoEnviadoAsync(pedidoId, HttpContext.RequestAborted);
            _logger.LogInformation("Pedido {PedidoId} marcado Enviado por {Admin}", pedidoId, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Estado del pedido actualizado a Enviado");
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Repartidor")]
        public async Task<IActionResult> Entregar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _estadosPedidoService.PedidoEntregadoAsync(pedidoId, HttpContext.RequestAborted);
            _logger.LogInformation("Pedido {PedidoId} marcado Entregado por {Admin}", pedidoId, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Estado del pedido actualizado a Entregado");
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }
            await _estadosPedidoService.PedidoCanceladoAsync(pedidoId, HttpContext.RequestAborted);
            _logger.LogInformation("Pedido {PedidoId} cancelado por {Admin} (con devolución de stock si estaba pagado)", pedidoId, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Estado del pedido actualizado a Cancelado");
            return RedirectToAction("Detalles", new { id = pedidoId });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarPago(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }

            try
            {
                await _gestionTransferenciaService.AprobarPagoTransferenciaAsync(pedidoId, HttpContext.RequestAborted);
                _logger.LogInformation("Pago por transferencia aprobado para el pedido {PedidoId} por {Admin}", pedidoId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData.SetMensajeExito("Pago aprobado: el pedido pasó a preparación");
            }
            catch (DomainException ex)
            {
                // Guard de dominio: mensaje curado para el admin (sin comprobante, ya aprobado, vencido...).
                _logger.LogWarning("No se pudo aprobar el pago por transferencia del pedido {PedidoId}: {Motivo}", pedidoId, ex.Message);
                TempData.SetMensajeError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo inesperado al aprobar el pago por transferencia del pedido {PedidoId}", pedidoId);
                TempData.SetMensajeError("No se pudo aprobar el pago. Revisá el detalle del pedido e intentá de nuevo.");
            }

            return RedirectToAction("Listado");
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarComprobante(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return NotFound();
            }

            try
            {
                await _gestionTransferenciaService.RechazarComprobanteTransferenciaAsync(pedidoId, HttpContext.RequestAborted);
                _logger.LogInformation("Comprobante rechazado del pedido {PedidoId} por {Admin} (pedido cancelado y cliente notificado)", pedidoId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData.SetMensajeExito("Comprobante rechazado: pedido cancelado y cliente notificado");
            }
            catch (DomainException ex)
            {
                // Guard de dominio: mensaje curado para el admin (ya cancelado, sin comprobante...).
                _logger.LogWarning("No se pudo rechazar el comprobante del pedido {PedidoId}: {Motivo}", pedidoId, ex.Message);
                TempData.SetMensajeError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo inesperado al rechazar el comprobante del pedido {PedidoId}", pedidoId);
                TempData.SetMensajeError("No se pudo rechazar el comprobante. Revisá el detalle del pedido e intentá de nuevo.");
            }

            return RedirectToAction("Listado");
        }
    }
}
