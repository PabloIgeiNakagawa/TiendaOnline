using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Enums;

namespace TiendaOnline.Features.Pedidos
{
    [Route("[controller]")]
    public class PedidosController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IPedidoCommandService _pedidoCommandService;
        private readonly ICarritoService _carritoService;
        private readonly IDireccionService _direccionService;
        private readonly IPaymentService _paymentService;
        private readonly IComprobantePdfService _comprobantePdfService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService, ICarritoService carritoService, IDireccionService direccionService, IPaymentService paymentService, IComprobantePdfService comprobantePdfService, IAppSettingsService appSettingsService, ILogger<PedidosController> logger)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
            _carritoService = carritoService;
            _direccionService = direccionService;
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
                        Estado = (EstadoPedidoUI)p.EstadoId,
                        EstadoPago = (EstadoPagoUI)p.EstadoPagoId
                    })
                    .ToList()
            };

            return View(viewmodel);
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
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
                Estado = (EstadoPedidoUI)pedido.EstadoId,
                EstadoPago = (EstadoPagoUI)pedido.EstadoPagoId,
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
                TempData["MensajeError"] = "Adjuntá el comprobante para poder verificarlo.";
                return RedirectToAction("Instrucciones", new { id });
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!extensionesValidas.Contains(extension))
            {
                TempData["MensajeError"] = "Formato no admitido. Usá JPG, PNG o PDF.";
                return RedirectToAction("Instrucciones", new { id });
            }

            if (archivo.Length > TamanoMaximoComprobante)
            {
                TempData["MensajeError"] = "El archivo supera el máximo de 5 MB.";
                return RedirectToAction("Instrucciones", new { id });
            }

            try
            {
                await using var stream = archivo.OpenReadStream();
                await _pedidoCommandService.GuardarComprobanteAsync(id, stream, $"Pedido-{id:D6}{extension}", HttpContext.RequestAborted);
                _logger.LogInformation("Comprobante subido para el pedido {PedidoId}", id);
                TempData["MensajeExito"] = "Comprobante recibido. Te avisamos cuando verifiquemos el pago.";
            }
            catch (DomainException ex)
            {
                // Guard de dominio (pedido equivocado, comprobante repetido, etc.): mensaje curado del service.
                _logger.LogWarning(ex, "No se pudo registrar el comprobante del pedido {PedidoId}: {Motivo}", id, ex.Message);
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction("Instrucciones", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la subida del comprobante del pedido {PedidoId}", id);
                TempData["MensajeError"] = "No pudimos subir el comprobante. Intentá de nuevo en unos minutos.";
                return RedirectToAction("Instrucciones", new { id });
            }

            return RedirectToAction("Detalles", new { id });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> CheckOut()
        {
            // Validamos ANTES de leer el carrito: ValidarStock refresca precio y stock
            // contra la BD; si algo cambió, avisamos y volvemos al carrito.
            var validacion = await _carritoService.ValidarStockAsync(HttpContext.RequestAborted);
            if (validacion.IsFailure)
            {
                TempData["MensajeError"] = "No pudimos validar tu carrito. Probá de nuevo.";
                return RedirectToAction("Index", "Carritos");
            }

            if (!validacion.Value.TodoOK)
            {
                TempData["MensajeError"] = ArmarMensajeStockInsuficiente(validacion.Value.ItemsSinStock);
                return RedirectToAction("Index", "Carritos");
            }

            // Recién acá leemos el carrito YA refrescado: subtotal con el precio vigente
            var items = await _carritoService.ObtenerAsync();

            if (items == null || items.Count == 0)
            {
                TempData["MensajeError"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Carritos");
            }

            var subtotal = items.Sum(i => i.Precio * i.Cantidad);

            var direcciones = await _direccionService.ObtenerDireccionesAsync(ObtenerUsuarioId(), HttpContext.RequestAborted);

            var viewModel = new CheckOutViewModel
            {
                DireccionesGuardadas = direcciones.Select(d => new DireccionGuardadaViewModel
                {
                    DireccionId = d.DireccionId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero,
                    Piso = d.Piso,
                    Departamento = d.Departamento,
                    Localidad = d.Localidad,
                    Provincia = d.Provincia,
                    CodigoPostal = d.CodigoPostal
                }).ToList(),
                Items = items.Select(i => new CheckOutItemViewModel
                {
                    ProductoId = i.ProductoId,
                    Nombre = i.Nombre,
                    Precio = i.Precio,
                    PrecioLista = i.PrecioLista,
                    Cantidad = i.Cantidad,
                    ImagenUrl = i.ImagenUrl
                }).ToList(),
                SubTotal = subtotal,
                CostoEnvio = 0
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("checkout")]
        public async Task<IActionResult> ProcesarCheckout(CheckOutViewModel model)
        {
            // RECARGAR DATOS DEL CARRITO (Vital para que no se pierdan los Items)
            var carritoItems = await _carritoService.ObtenerAsync();

            // Mapeamos los items del servicio al ViewModel porque el POST los trajo nulos
            model.Items = carritoItems.Select(i => new CheckOutItemViewModel
            {
                ProductoId = i.ProductoId,
                Nombre = i.Nombre,
                Precio = i.Precio,
                PrecioLista = i.PrecioLista,
                Cantidad = i.Cantidad,
                ImagenUrl = i.ImagenUrl
            }).ToList();

            model.SubTotal = model.Items.Sum(i => i.Precio * i.Cantidad);
            // Falta asignar costo de envío según lógica de negocio
            model.CostoEnvio = model.MetodoEntrega == MetodoEntrega.EnvioDomicilio ? 500 : 0;

            // Si es retiro en local, ignoramos la validación de la dirección
            if (model.MetodoEntrega == MetodoEntrega.RetiroLocal)
            {
                // Eliminamos todos los errores que empiecen con "NuevaDireccion"
                foreach (var key in ModelState.Keys.Where(k => k.StartsWith("NuevaDireccion")).ToList())
                {
                    ModelState.Remove(key);
                }

                // También si tienes validación para la dirección guardada
                ModelState.Remove(nameof(model.DireccionSeleccionadaId));
            }

            // Lógica de validación
            if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
            {
                // SI ELIGIÓ UNA GUARDADA: Ignoramos los errores de validación de la "NuevaDireccion"
                if (model.DireccionSeleccionadaId != null)
                {
                    // Buscamos todas las llaves que empiecen con "NuevaDireccion" y las removemos del ModelState
                    var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("NuevaDireccion")).ToList();
                    foreach (var key in keysToRemove)
                    {
                        ModelState.Remove(key);
                    }
                }
                else if (string.IsNullOrEmpty(model.NuevaDireccion?.Calle))
                {
                    ModelState.AddModelError("", "Debes seleccionar o ingresar una dirección.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Si hay errores, las direcciones guardadas también se pierden, hay que recargarlas
                var dir = await _direccionService.ObtenerDireccionesAsync(ObtenerUsuarioId(), HttpContext.RequestAborted);
                model.DireccionesGuardadas = dir.Select(d => new DireccionGuardadaViewModel
                {
                    DireccionId = d.DireccionId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero
                }).ToList();

                return View("CheckOut", model);
            }

            var modelJson = System.Text.Json.JsonSerializer.Serialize(model);
            TempData["CheckOutTemp"] = modelJson;
            return RedirectToAction("Confirmacion");
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Confirmacion()
        {
            var modelJson = TempData["CheckOutTemp"] as string;
            if (string.IsNullOrEmpty(modelJson))
                return RedirectToAction("CheckOut");

            var model = System.Text.Json.JsonSerializer.Deserialize<CheckOutViewModel>(modelJson);
            if (model == null)
                return RedirectToAction("CheckOut");

            var carritoItems = await _carritoService.ObtenerAsync();

            var nombre = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var apellido = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var telefono = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty;

            var viewModel = new ConfirmacionPedidoViewModel
            {
                Items = carritoItems,
                SubTotal = model.SubTotal,
                CostoEnvio = model.CostoEnvio,
                MetodoEntrega = model.MetodoEntrega,
                NombreUsuario = $"{nombre} {apellido}",
                EmailUsuario = email,
                TelefonoUsuario = telefono
            };

            // Lógica para la dirección a mostrar
            if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
            {
                if (model.DireccionSeleccionadaId != null)
                {
                    var dir = await _direccionService.ObtenerPorIdAsync(model.DireccionSeleccionadaId, HttpContext.RequestAborted);
                    viewModel.Direccion = new DireccionCheckOut
                    {
                        EsNueva = false,
                        Etiqueta = dir.Etiqueta,
                        Provincia = dir.Provincia,
                        Localidad = dir.Localidad,
                        CodigoPostal = dir.CodigoPostal,
                        Calle = dir.Calle,
                        Numero = dir.Numero,
                        Piso = dir.Piso,
                        Departamento = dir.Departamento,
                        Observaciones = dir.Observaciones ?? string.Empty
                    };
                }
                else
                {
                    var d = model.NuevaDireccion;
                    viewModel.Direccion = new DireccionCheckOut
                    {
                        EsNueva = true,
                        Etiqueta = d.Etiqueta,
                        Provincia = d.Provincia,
                        Localidad = d.Localidad,
                        CodigoPostal = d.CodigoPostal,
                        Calle = d.Calle,
                        Numero = d.Numero,
                        Piso = d.Piso,
                        Departamento = d.Departamento,
                        Observaciones = d.Observaciones ?? string.Empty
                    };
                }
            }

            return View(viewModel);
        }

        [Authorize]
        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("checkout")]
        public async Task<IActionResult> FinalizarCompra(ConfirmacionPedidoViewModel model, MetodoPagoId metodoDePagoId)
        {
            var usuarioId = ObtenerUsuarioId();

            // Recargamos los items del carrito (vital porque el POST no los envía)
            var carritoItems = await _carritoService.ObtenerAsync();

            if (model == null || carritoItems == null || !carritoItems.Any())
            {
                TempData["MensajeError"] = "La sesión de compra expiró o los datos son inválidos.";
                return RedirectToAction("CheckOut");
            }

            // Última barrera antes de cobrar: stock y precio recién refrescados contra BD
            // (el POST no confía en lo que le llegó). Si algo no aguanta, al carrito.
            var validacion = await _carritoService.ValidarStockAsync(HttpContext.RequestAborted);
            if (validacion.IsFailure)
            {
                TempData["MensajeError"] = "No pudimos validar tu carrito. Probá de nuevo.";
                return RedirectToAction("Index", "Carritos");
            }

            if (!validacion.Value.TodoOK)
            {
                TempData["MensajeError"] = ArmarMensajeStockInsuficiente(validacion.Value.ItemsSinStock);
                return RedirectToAction("Index", "Carritos");
            }

            // Releemos el carrito YA refrescado por la validación (precio vigente)
            carritoItems = await _carritoService.ObtenerAsync();

            // Para envío a domicilio, la dirección es obligatoria
            if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio && model.Direccion == null)
            {
                TempData["MensajeError"] = "La dirección de envío es requerida.";
                return RedirectToAction("CheckOut");
            }

            var dto = new CrearPedidoDto
            {
                UsuarioId = usuarioId,
                MetodoDePagoId = (int)metodoDePagoId,
                EsEnvioADomicilio = model.MetodoEntrega == MetodoEntrega.EnvioDomicilio,
                EnvioCalle = model.Direccion?.Calle,
                EnvioNumero = model.Direccion?.Numero,
                EnvioPiso = model.Direccion?.Piso,
                EnvioDepartamento = model.Direccion?.Departamento,
                EnvioObservaciones = model.Direccion?.Observaciones,
                EnvioLocalidad = model.Direccion?.Localidad,
                EnvioProvincia = model.Direccion?.Provincia,
                EnvioCodigoPostal = model.Direccion?.CodigoPostal,
                CostoEnvio = model.MetodoEntrega == MetodoEntrega.EnvioDomicilio ? 500 : 0,
                Items = carritoItems.Select(i => new CrearPedidoDetalleDto
                {
                    ProductoId = i.ProductoId,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.Precio
                }).ToList()
            };

            try
            {
                // Crear el pedido en BD
                var pedido = await _pedidoCommandService.CrearPedidoYPrepararPagoAsync(dto, HttpContext.RequestAborted);

                if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio && model.Direccion?.EsNueva == true)
                {
                    // Si es envío a domicilio, guardamos la dirección para el usuario
                    await _direccionService.GuardarDireccionAsync(usuarioId, new DireccionDto
                    {
                        Etiqueta = model.Direccion.Etiqueta,
                        Calle = model.Direccion.Calle,
                        Numero = model.Direccion.Numero,
                        Piso = model.Direccion.Piso,
                        Departamento = model.Direccion.Departamento,
                        Localidad = model.Direccion.Localidad,
                        Provincia = model.Direccion.Provincia,
                        CodigoPostal = model.Direccion.CodigoPostal,
                        Observaciones = model.Direccion.Observaciones
                    }, HttpContext.RequestAborted);
                }

                // Si el método es Mercado Pago (ID 1), generamos link y salimos
                if (metodoDePagoId == MetodoPagoId.MercadoPago)
                {
                    var urlPago = await _paymentService.GenerarPreferenciaPagoAsync(pedido, HttpContext.RequestAborted);
                    return Redirect(urlPago);
                }

                // Transferencia: directo a la página con CBU/alias y la subida del comprobante
                await _carritoService.VaciarAsync();
                TempData["MensajeExito"] = "¡Pedido realizado con éxito!";
                return RedirectToAction("Instrucciones", new { id = pedido.PedidoId });
            }
            catch (DomainException ex)
            {
                // Guard de dominio (estado inválido, producto inexistente, etc.): mensaje curado del service.
                _logger.LogWarning(ex, "Checkout rechazado por reglas de negocio (usuario {UsuarioId}): {Motivo}", usuarioId, ex.Message);
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction("Index", "Carritos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo el checkout para el usuario {UsuarioId}", usuarioId);
                TempData["MensajeError"] = "No pudimos generar el pedido. Revisá tu carrito e intentá de nuevo.";
                return RedirectToAction("Index", "Carritos");
            }
        }

        // Mensaje único para TempData: lista los productos con stock insuficiente y cuánto quedó.
        private static string ArmarMensajeStockInsuficiente(List<StockInsuficienteDto> itemsSinStock)
            => "Hay productos con stock insuficiente: "
               + string.Join(
                   ", ",
                   itemsSinStock.Select(i => i.StockDisponible == 0
                       ? $"{i.Nombre} (sin stock)"
                       : $"{i.Nombre} (quedan {i.StockDisponible})"))
               + ". Revisá tu carrito para ajustar cantidades.";

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
                    var pagoConfirmado = await _pedidoCommandService.ConfirmarPagoAsync(infoPago, HttpContext.RequestAborted);
                    if (pagoConfirmado)
                    {
                        await _carritoService.VaciarAsync();
                        TempData["MensajeExito"] = "¡Pago confirmado exitosamente!";
                    }
                    else
                    {
                        TempData["MensajeError"] = "El pago fue recibido, pero el pedido ya no pudo confirmarse automáticamente. Contactanos para revisarlo.";
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
        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReintentarPago(int pedidoId)
        {
            // Buscamos el pedido existente con sus datos
            var pedidoDto = await _pedidoCommandService.ObtenerDatosParaPagoAsync(pedidoId, HttpContext.RequestAborted);

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
            if (comprobanteDto.EstadoPagoId != (int)EstadoPagoUI.Aprobado)
            {
                TempData["MensajeError"] = "El comprobante solo está disponible para pagos aprobados.";
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
                TempData["MensajeError"] = "No se pudo generar el comprobante. Intente nuevamente más tarde.";
                return RedirectToAction("Detalles", new { id });
            }
        }

        private int ObtenerUsuarioId()
        {
            // Buscamos el claim de tipo NameIdentifier (que es el ID en Identity)
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null) throw new UnauthorizedAccessException();

            return int.Parse(claim.Value);
        }
    }
}
