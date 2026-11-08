using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Pedidos.Common;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Enums;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Pedidos
{
    [Route("[controller]")]
    public class PedidosController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IConfirmacionPagoService _confirmacionPagoService;
        private readonly IGestionTransferenciaService _gestionTransferenciaService;
        private readonly ICarritoService _carritoService;
        private readonly IPaymentService _paymentService;
        private readonly IComprobantePdfService _comprobantePdfService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(IPedidoQueryService pedidoQueryService, IConfirmacionPagoService confirmacionPagoService, IGestionTransferenciaService gestionTransferenciaService, ICarritoService carritoService, IPaymentService paymentService, IComprobantePdfService comprobantePdfService, IAppSettingsService appSettingsService, ILogger<PedidosController> logger)
        {
            _pedidoQueryService = pedidoQueryService;
            _confirmacionPagoService = confirmacionPagoService;
            _gestionTransferenciaService = gestionTransferenciaService;
            _carritoService = carritoService;
            _paymentService = paymentService;
            _comprobantePdfService = comprobantePdfService;
            _appSettingsService = appSettingsService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> MisPedidos()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                return Unauthorized();
            }

            int usuarioId = int.Parse(claim.Value);

            var pedidos = await _pedidoQueryService.ObtenerPedidosDeUsuarioAsync(usuarioId, HttpContext.RequestAborted);

            var viewmodel = new MisPedidosViewModel
            {
                Pedidos = pedidos
                    .OrderByDescending(p => p.FechaPedido)
                    .Select(p => new PedidoListaViewModel
                    {
                        PedidoId = p.PedidoId,
                        FechaPedido = p.FechaPedido,
                        FechaEnvio = p.FechaEnvio,
                        FechaEntrega = p.FechaEntrega,
                        FechaCancelado = p.FechaCancelado,
                        Productos = p.Productos,
                        Estado = (EstadoPedido)p.EstadoId,
                        EstadoPago = (EstadoPago)p.EstadoPagoId
                    })
                    .ToList()
            };

            return View(viewmodel);
        }

        [Authorize(Policy = "VeDetallesDelPedido")]
        [HttpGet("Detalles/{id}")]
        public async Task<IActionResult> Detalles(int id)
        {
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(id, HttpContext.RequestAborted);
            if (pedido == null)
                return NotFound();

            var subtotal = pedido.Items.Sum(d => d.Cantidad * d.PrecioUnitario);

            var viewModel = new PedidoDetalleViewModel
            {
                PedidoId = pedido.PedidoId,
                FechaPedido = pedido.FechaPedido,
                FechaEnvio = pedido.FechaEnvio,
                FechaEntrega = pedido.FechaEntrega,
                FechaCancelado = pedido.FechaCancelado,
                Estado = (EstadoPedido)pedido.EstadoId,
                EstadoPago = (EstadoPago)pedido.EstadoPagoId,
                MetodoPagoId = (MetodoPagoId)pedido.MetodoPagoId,

                UsuarioNombre = pedido.UsuarioNombre,
                UsuarioEmail = pedido.UsuarioEmail,
                UsuarioTelefono = pedido.UsuarioTelefono,

                DireccionCompleta = pedido.DireccionCompleta,
                Observaciones = pedido.Observaciones,

                Items = pedido.Items.Select(d => new PedidoItemViewModel
                {
                    ProductoNombre = d.ProductoNombre,
                    ProductoImagenUrl = d.ProductoImagenUrl,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList(),

                Subtotal = subtotal,
                Total = subtotal + pedido.CostoEnvio,
                CostoEnvio = pedido.CostoEnvio,

                TransaccionPagoId = pedido.TransaccionPagoId,
                ComprobanteUrl = pedido.ComprobanteUrl,
                FechaComprobante = pedido.FechaComprobante,

                EsAdmin = User.IsInRole("Administrador"),
                EsRepartidor = User.IsInRole("Repartidor"),
                EsPropioPedido = pedido.UsuarioId.ToString() ==
                                 User.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            return View(viewModel);
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
        [HttpGet("[action]")]
        public async Task<IActionResult> Instrucciones(int id)
        {
            var datos = await _pedidoQueryService.ObtenerInstruccionesTransferenciaAsync(id, HttpContext.RequestAborted);
            if (datos == null) return NotFound();

            var viewModel = new InstruccionesTransferenciaViewModel
            {
                PedidoId = datos.PedidoId,
                Total = datos.Total,
                ComprobanteUrl = datos.ComprobanteUrl,
                PuedeSubirComprobante = datos.PuedeSubirComprobante,
                Vencido = datos.Vencido,
                Cbu = _appSettingsService.GetValue("Transferencia:Cbu") ?? string.Empty,
                Alias = _appSettingsService.GetValue("Transferencia:Alias") ?? string.Empty,
                Titular = _appSettingsService.GetValue("Transferencia:Titular") ?? string.Empty
            };

            return View(viewModel);
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("subir-comprobante")]
        public async Task<IActionResult> SubirComprobante(int id, IFormFile archivo)
        {
            const int TamanoMaximoComprobante = 5 * 1024 * 1024; // 5 MB
            string[] extensionesValidas = [".jpg", ".jpeg", ".png", ".pdf"];

            if (archivo == null || archivo.Length == 0)
            {
                TempData.SetMensajeError("Adjuntá el comprobante para poder verificarlo.");
                return RedirectToAction("Instrucciones", new { id });
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!extensionesValidas.Contains(extension))
            {
                TempData.SetMensajeError("Formato no admitido. Usá JPG, PNG o PDF.");
                return RedirectToAction("Instrucciones", new { id });
            }

            if (archivo.Length > TamanoMaximoComprobante)
            {
                TempData.SetMensajeError("El archivo supera el máximo de 5 MB.");
                return RedirectToAction("Instrucciones", new { id });
            }

            try
            {
                await using var stream = archivo.OpenReadStream();
                await _gestionTransferenciaService.GuardarComprobanteAsync(id, stream, $"Pedido-{id:D6}{extension}", HttpContext.RequestAborted);
                _logger.LogInformation("Comprobante subido para el pedido {PedidoId}", id);
                TempData.SetMensajeExito("Comprobante recibido. Te avisamos cuando verifiquemos el pago.");
            }
            catch (DomainException ex)
            {
                // Guard de dominio (pedido equivocado, comprobante repetido, etc.): mensaje curado del service.
                _logger.LogWarning(ex, "No se pudo registrar el comprobante del pedido {PedidoId}: {Motivo}", id, ex.Message);
                TempData.SetMensajeError(ex.Message);
                return RedirectToAction("Instrucciones", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la subida del comprobante del pedido {PedidoId}", id);
                TempData.SetMensajeError("No pudimos subir el comprobante. Intentá de nuevo en unos minutos.");
                return RedirectToAction("Instrucciones", new { id });
            }

            return RedirectToAction("Detalles", new { id });
        }

        // Mensaje del front según el motivo con el que el pago no pudo confirmarse.
        private static string MotivoPagoALegible(MotivoPago motivo) => motivo switch
        {
            MotivoPago.MontoNoCoincide => "Recibimos el pago con un monto distinto al del pedido. Contactanos para revisarlo.",
            MotivoPago.StockInsuficiente => "El pago llegó, pero ya no hay stock del pedido. Contactanos para coordinar la solución.",
            MotivoPago.PedidoNoEncontrado => "No encontramos el pedido asociado al pago. Contactanos para revisarlo.",
            MotivoPago.PedidoVencido => "El pago llegó fuera del plazo del pedido. Contactanos para revisarlo.",
            _ => "El pago llegó cuando el pedido ya estaba procesado. Si no coincide con lo que ves, contactanos."
        };

        [Authorize(Policy = "EsDuenioDelPedido")]
        [HttpGet("[action]")]
        public async Task<IActionResult> PagoExitoso([FromQuery] string external_reference, [FromQuery] string payment_id)
        {
            // Confirmar el pago manualmente si el webhook no llegó
            if (!string.IsNullOrEmpty(payment_id))
            {
                var infoPago = await _paymentService.ObtenerDetallesPagoAsync(payment_id, HttpContext.RequestAborted);
                if (infoPago != null && infoPago.Estado == "approved")
                {
                    var resultadoPago = await _confirmacionPagoService.ConfirmarPagoAsync(infoPago, HttpContext.RequestAborted);
                    // Confirmado y YaConfirmado son las dos únicas salidas en las que el pago está aprobado acá.
                    if (resultadoPago.Confirmado)
                    {
                        await _carritoService.VaciarAsync();
                        TempData.SetMensajeExito(resultadoPago.Motivo == MotivoPago.YaConfirmado
                            ? "Tu pago ya estaba confirmado."
                            : "¡Pago confirmado exitosamente!");
                    }
                    else
                    {
                        TempData.SetMensajeError(MotivoPagoALegible(resultadoPago.Motivo));
                    }
                }
            }

            ViewBag.PedidoId = external_reference;
            ViewBag.PagoId = payment_id;

            return View();
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
        [HttpGet("[action]")]
        public IActionResult PagoFallido([FromQuery] string external_reference)
        {
            ViewBag.PedidoId = external_reference;
            return View();
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
        [HttpPost("[action]/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReintentarPago(int id)
        {
            // El id va por route: es el único valor que el policy handler resuelve
            // (vivo en el body del form, el endpoint respondía 403 siempre).
            var pedidoDto = await _confirmacionPagoService.ObtenerDatosParaPagoAsync(id, HttpContext.RequestAborted);

            if (pedidoDto == null) return NotFound();

            // Generamos un nuevo link de Mercado Pago
            var urlPago = await _paymentService.GenerarPreferenciaPagoAsync(pedidoDto, HttpContext.RequestAborted);

            // Mandamos al usuario a pagar otra vez
            return Redirect(urlPago);
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
        [EnableRateLimiting("comprobante")]
        [HttpGet("[action]")]
        public async Task<IActionResult> ComprobantePDF(int id)
        {
            var comprobanteDto = await _pedidoQueryService.ObtenerComprobanteDtoAsync(id, HttpContext.RequestAborted);
            if (comprobanteDto == null)
            {
                return NotFound();
            }

            // Solo generar PDF si el pago está aprobado
            if (comprobanteDto.EstadoPagoId != (int)EstadoPago.Aprobado)
            {
                TempData.SetMensajeError("El comprobante solo está disponible para pagos aprobados.");
                return RedirectToAction("Detalles", new { id });
            }

            try
            {
                var pdfBytes = await _comprobantePdfService.GenerarComprobanteAsync(comprobanteDto);

                return File(pdfBytes, "application/pdf", $"Comprobante_Pedido_{id:D6}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando comprobante PDF para pedido {PedidoId}", id);
                TempData.SetMensajeError("No se pudo generar el comprobante. Intente nuevamente más tarde.");
                return RedirectToAction("Detalles", new { id });
            }
        }
    }
}
