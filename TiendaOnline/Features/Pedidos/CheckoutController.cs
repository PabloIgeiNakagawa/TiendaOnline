using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Envios;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Enums;

namespace TiendaOnline.Features.Pedidos
{
    [Route("[controller]")]
    public class CheckoutController : Controller
    {
        private readonly ICarritoService _carritoService;
        private readonly IDireccionService _direccionService;
        private readonly IPedidoCommandService _pedidoCommandService;
        private readonly IPaymentService _paymentService;
        private readonly IEnvioQueryService _envioQueryService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ICarritoService carritoService, IDireccionService direccionService, IPedidoCommandService pedidoCommandService, IPaymentService paymentService, IEnvioQueryService envioQueryService, ILogger<CheckoutController> logger)
        {
            _carritoService = carritoService;
            _direccionService = direccionService;
            _pedidoCommandService = pedidoCommandService;
            _paymentService = paymentService;
            _envioQueryService = envioQueryService;
            _logger = logger;
        }

        // Cotización en vivo para el front: el POST nunca define el precio, esto es solo display.
        [HttpGet("[action]")]
        public async Task<IActionResult> CotizarEnvio([FromQuery] string provincia)
        {
            var cotizacion = await _envioQueryService.CotizarEnvioAsync(provincia, HttpContext.RequestAborted);
            return Ok(new { cotizacion.Costo, cotizacion.Permitida });
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

            // El costo de envío sale de la tabla de zonas (editable del panel); retiro local = $0.
            model.CostoEnvio = 0;
            if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
            {
                var provincia = await ObtenerProvinciaDeEntregaAsync(model);
                if (!string.IsNullOrWhiteSpace(provincia))
                {
                    var cotizacion = await _envioQueryService.CotizarEnvioAsync(provincia, HttpContext.RequestAborted);
                    if (cotizacion.Permitida)
                    {
                        model.CostoEnvio = cotizacion.Costo;
                    }
                    else
                    {
                        ModelState.AddModelError("", $"No hacemos envíos a {provincia}. Elegí otra dirección o retirá en el local.");
                    }
                }
                // Sin provincia aún (dirección incompleta): el costo queda en 0 y la
                // validación de ModelState ya exige la dirección como antes.
            }

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

            // El precio nunca lo define el POST: el costo de envío se re-cotiza acá
            // contra la zona y de este valor sale el Total del pedido que se cobra.
            decimal costoEnvio = 0;
            if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
            {
                var cotizacion = await _envioQueryService.CotizarEnvioAsync(model.Direccion?.Provincia, HttpContext.RequestAborted);
                if (!cotizacion.Permitida)
                {
                    TempData["MensajeError"] = $"No hacemos envíos a {model.Direccion?.Provincia}. Vuelve al checkout y elegí retiro en el local u otra dirección.";
                    return RedirectToAction("CheckOut");
                }
                costoEnvio = cotizacion.Costo;
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
                CostoEnvio = costoEnvio,
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
                return RedirectToAction("Instrucciones", "Pedidos", new { id = pedido.PedidoId });
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

        // Provincia de la dirección de entrega: guardada (la leemos de la DB, no del POST)
        // o nueva (la que el usuario completó en el formulario del checkout).
        private async Task<string?> ObtenerProvinciaDeEntregaAsync(CheckOutViewModel model)
        {
            if (model.DireccionSeleccionadaId != null)
            {
                var dir = await _direccionService.ObtenerPorIdAsync(model.DireccionSeleccionadaId, HttpContext.RequestAborted);
                return dir?.Provincia;
            }

            return model.NuevaDireccion?.Provincia;
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
