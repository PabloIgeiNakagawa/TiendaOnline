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
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Pedidos
{
    [Route("[controller]")]
    public class CheckoutController : Controller
    {
        private readonly ICarritoService _carritoService;
        private readonly IDireccionService _direccionService;
        private readonly ICreacionPedidoService _creacionPedidoService;
        private readonly IPaymentService _paymentService;
        private readonly IEnvioQueryService _envioQueryService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ICarritoService carritoService, IDireccionService direccionService, ICreacionPedidoService creacionPedidoService, IPaymentService paymentService, IEnvioQueryService envioQueryService, ILogger<CheckoutController> logger)
        {
            _carritoService = carritoService;
            _direccionService = direccionService;
            _creacionPedidoService = creacionPedidoService;
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
                TempData.SetMensajeError("No pudimos validar tu carrito. Probá de nuevo.");
                return RedirectToAction("Index", "Carritos");
            }

            if (!validacion.Value.TodoOK)
            {
                TempData.SetMensajeError(ArmarMensajeStockInsuficiente(validacion.Value.ItemsSinStock));
                return RedirectToAction("Index", "Carritos");
            }

            // Recién acá leemos el carrito YA refrescado: subtotal con el precio vigente
            var items = await _carritoService.ObtenerAsync();

            if (items == null || items.Count == 0)
            {
                TempData.SetMensajeError("Tu carrito está vacío.");
                return RedirectToAction("Index", "Carritos");
            }

            var viewModel = new CheckOutViewModel
            {
                DireccionesGuardadas = MapearDireccionesGuardadas(
                    await _direccionService.ObtenerDireccionesAsync(ObtenerUsuarioId(), HttpContext.RequestAborted)),
                Items = MapearItems(items),
                SubTotal = items.Sum(i => i.Precio * i.Cantidad),
                CostoEnvio = 0, // display: el JS lo cotiza en vivo; el server re-cotiza al POST
                MetodoPagoId = MetodoPagoId.MercadoPago // pre-seleccionado como en la vista
            };

            return View(viewModel);
        }

        // Wizard de una sola página: entrega + pago en un único POST (el paso de
        // Confirmación desapareció). El carrito vivo del service es la única fuente
        // de items: el POST no trae productos, ni precios, ni dinero.
        [Authorize]
        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("checkout")]
        public async Task<IActionResult> FinalizarCompra(CheckOutViewModel model)
        {
            var usuarioId = ObtenerUsuarioId();

            var carritoItems = await _carritoService.ObtenerAsync();
            if (carritoItems == null || carritoItems.Count == 0)
            {
                TempData.SetMensajeError("Tu carrito está vacío.");
                return RedirectToAction("Index", "Carritos");
            }

            // Última barrera antes de cobrar: stock y precio recién refrescados contra BD
            // (el POST no confía en lo que le llegó). Si algo no aguanta, al carrito.
            var validacion = await _carritoService.ValidarStockAsync(HttpContext.RequestAborted);
            if (validacion.IsFailure)
            {
                TempData.SetMensajeError("No pudimos validar tu carrito. Probá de nuevo.");
                return RedirectToAction("Index", "Carritos");
            }

            if (!validacion.Value.TodoOK)
            {
                TempData.SetMensajeError(ArmarMensajeStockInsuficiente(validacion.Value.ItemsSinStock));
                return RedirectToAction("Index", "Carritos");
            }

            // Recién acá leemos el carrito YA refrescado por la validación (precio vigente)
            carritoItems = await _carritoService.ObtenerAsync();

            // Retiro en local no exige dirección; envío a domicilio exige una guardada
            // o una nueva con datos (los campos de la otra rama salen del ModelState).
            if (model.MetodoEntrega == MetodoEntrega.RetiroLocal)
            {
                QuitarErroresDeNuevaDireccion();
                ModelState.Remove(nameof(model.DireccionSeleccionadaId));
            }
            else if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
            {
                if (model.DireccionSeleccionadaId != null)
                {
                    QuitarErroresDeNuevaDireccion();
                }
                else if (string.IsNullOrEmpty(model.NuevaDireccion?.Calle))
                {
                    ModelState.AddModelError(string.Empty, "Debes seleccionar o ingresar una dirección.");
                }
            }

            // El default del enum (0) no es un método real: exigimos uno conocido.
            if (model.MetodoPagoId is not (MetodoPagoId.MercadoPago or MetodoPagoId.Transferencia))
            {
                ModelState.AddModelError(nameof(model.MetodoPagoId), "Seleccioná un método de pago.");
            }

            if (!ModelState.IsValid)
            {
                // Items y direcciones no viajan en el POST: se recargan para re-renderizar.
                return await VistaConDatosRecargadosAsync(model);
            }

            // Dirección de entrega: guardada → leída del usuario en la BD (ownership
            // server-side; el id del POST no es autorización); nueva → los datos del
            // form. El costo de envío NUNCA lo define el POST: se re-cotiza acá contra
            // la zona y de este valor sale el Total del pedido que se cobra.
            DireccionDeEntrega? entrega = null;
            decimal costoEnvio = 0;
            if (model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
            {
                entrega = model.DireccionSeleccionadaId != null
                    ? await LeerDireccionGuardadaAsync(model.DireccionSeleccionadaId.Value, usuarioId)
                    : DireccionDeEntrega.DelFormulario(model.NuevaDireccion!);

                if (entrega == null)
                {
                    // Guardada borrada o ajena a este usuario: frenamos con mensaje
                    // claro — nunca pasamos con costo 0 fingiendo que la dirección vale.
                    TempData.SetMensajeError("La dirección seleccionada ya no está disponible. Elegí otra en el checkout.");
                    return RedirectToAction(nameof(CheckOut));
                }

                var cotizacion = await _envioQueryService.CotizarEnvioAsync(entrega.Provincia, HttpContext.RequestAborted);
                if (!cotizacion.Permitida)
                {
                    TempData.SetMensajeError($"No hacemos envíos a {entrega.Provincia}. Vuelve al checkout y elegí retiro en el local u otra dirección.");
                    return RedirectToAction(nameof(CheckOut));
                }
                costoEnvio = cotizacion.Costo;
            }

            var dto = new CrearPedidoDto
            {
                UsuarioId = usuarioId,
                MetodoDePagoId = (int)model.MetodoPagoId,
                EsEnvioADomicilio = model.MetodoEntrega == MetodoEntrega.EnvioDomicilio,
                EnvioCalle = entrega?.Calle,
                EnvioNumero = entrega?.Numero,
                EnvioPiso = entrega?.Piso,
                EnvioDepartamento = entrega?.Departamento,
                EnvioObservaciones = entrega?.Observaciones,
                EnvioLocalidad = entrega?.Localidad,
                EnvioProvincia = entrega?.Provincia,
                EnvioCodigoPostal = entrega?.CodigoPostal,
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
                var pedido = await _creacionPedidoService.CrearPedidoYPrepararPagoAsync(dto, HttpContext.RequestAborted);

                // Solo la dirección cargada nueva se guarda en el perfil; la guardada ya vive ahí.
                if (entrega != null && entrega.EsNueva)
                {
                    await _direccionService.GuardarDireccionAsync(usuarioId, new DireccionDto
                    {
                        Etiqueta = entrega.Etiqueta,
                        Calle = entrega.Calle,
                        Numero = entrega.Numero,
                        Piso = entrega.Piso,
                        Departamento = entrega.Departamento,
                        Localidad = entrega.Localidad,
                        Provincia = entrega.Provincia,
                        CodigoPostal = entrega.CodigoPostal,
                        Observaciones = entrega.Observaciones
                    }, HttpContext.RequestAborted);
                }

                // Mercado Pago: generamos link y salimos
                if (model.MetodoPagoId == MetodoPagoId.MercadoPago)
                {
                    var urlPago = await _paymentService.GenerarPreferenciaPagoAsync(pedido, HttpContext.RequestAborted);
                    return Redirect(urlPago);
                }

                // Transferencia: directo a la página con CBU/alias y la subida del comprobante
                await _carritoService.VaciarAsync();
                TempData.SetMensajeExito("¡Pedido realizado con éxito!");
                return RedirectToAction("Instrucciones", "Pedidos", new { id = pedido.PedidoId });
            }
            catch (DomainException ex)
            {
                // Guard de dominio (estado inválido, producto inexistente, etc.): mensaje curado del service.
                _logger.LogWarning(ex, "Checkout rechazado por reglas de negocio (usuario {UsuarioId}): {Motivo}", usuarioId, ex.Message);
                TempData.SetMensajeError(ex.Message);
                return RedirectToAction("Index", "Carritos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo el checkout para el usuario {UsuarioId}", usuarioId);
                TempData.SetMensajeError("No pudimos generar el pedido. Revisá tu carrito e intentá de nuevo.");
                return RedirectToAction("Index", "Carritos");
            }
        }

        // Datos de la entrega a domicilio: de una dirección guardada (leída del
        // usuario logueado — ajena/borrada → null y el caller informa) o de la
        // nueva que el usuario completó en el form. EsNueva decide si al cerrar el
        // pedido la dirección se guarda en el perfil.
        private sealed record DireccionDeEntrega(
            bool EsNueva,
            string Etiqueta,
            string Calle,
            string Numero,
            string? Piso,
            string? Departamento,
            string Localidad,
            string Provincia,
            string CodigoPostal,
            string? Observaciones)
        {
            public static DireccionDeEntrega DelFormulario(CheckOutDatosViewModel d) => new(
                EsNueva: true,
                Etiqueta: d.Etiqueta,
                Calle: d.Calle,
                Numero: d.Numero,
                Piso: d.Piso,
                Departamento: d.Departamento,
                Localidad: d.Localidad,
                Provincia: d.Provincia,
                CodigoPostal: d.CodigoPostal,
                Observaciones: d.Observaciones ?? string.Empty);
        }

        // Dirección guardada del usuario logueado: ajena/borrada → null (el caller
        // informa con mensaje amable). El id que decide es el del POST pero la
        // lectura solo alcanza sus direcciones.
        private async Task<DireccionDeEntrega?> LeerDireccionGuardadaAsync(int direccionId, int usuarioId)
        {
            var dir = await _direccionService.ObtenerDelUsuarioAsync(direccionId, usuarioId, HttpContext.RequestAborted);
            return dir == null ? null : new DireccionDeEntrega(
                EsNueva: false,
                Etiqueta: dir.Etiqueta,
                Calle: dir.Calle,
                Numero: dir.Numero,
                Piso: dir.Piso,
                Departamento: dir.Departamento,
                Localidad: dir.Localidad,
                Provincia: dir.Provincia,
                CodigoPostal: dir.CodigoPostal,
                Observaciones: dir.Observaciones ?? string.Empty);
        }

        private void QuitarErroresDeNuevaDireccion()
        {
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("NuevaDireccion")).ToList())
            {
                ModelState.Remove(key);
            }
        }

        // Re-render del wizard con errores de ModelState: items del carrito y
        // direcciones del perfil se recargan (el POST no los trae).
        private async Task<IActionResult> VistaConDatosRecargadosAsync(CheckOutViewModel model)
        {
            var items = await _carritoService.ObtenerAsync() ?? new List<ItemCarrito>();
            model.Items = MapearItems(items);
            model.SubTotal = items.Sum(i => i.Precio * i.Cantidad);
            model.CostoEnvio = 0; // display: sin re-cotización en el re-render

            model.DireccionesGuardadas = MapearDireccionesGuardadas(
                await _direccionService.ObtenerDireccionesAsync(ObtenerUsuarioId(), HttpContext.RequestAborted));

            return View("CheckOut", model);
        }

        private static List<CheckOutItemViewModel> MapearItems(List<ItemCarrito> items)
            => items.Select(i => new CheckOutItemViewModel
            {
                ProductoId = i.ProductoId,
                Nombre = i.Nombre,
                Precio = i.Precio,
                PrecioLista = i.PrecioLista,
                Cantidad = i.Cantidad,
                ImagenUrl = i.ImagenUrl
            }).ToList();

        private static List<DireccionGuardadaViewModel> MapearDireccionesGuardadas(List<DireccionDto> direcciones)
            => direcciones.Select(d => new DireccionGuardadaViewModel
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
            }).ToList();

        // Mensaje único para TempData: lista los productos con stock insuficiente y cuánto quedó.
        private static string ArmarMensajeStockInsuficiente(List<StockInsuficienteDto> itemsSinStock)
            => "Hay productos con stock insuficiente: "
               + string.Join(
                   ", ",
                   itemsSinStock.Select(i => i.StockDisponible == 0
                       ? $"{i.Nombre} (sin stock)"
                       : $"{i.Nombre} (quedan {i.StockDisponible})"))
               + ". Revisá tu carrito para ajustar cantidades.";

        private int ObtenerUsuarioId()
        {
            // Buscamos el claim de tipo NameIdentifier (que es el ID en Identity)
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null) throw new UnauthorizedAccessException();

            return int.Parse(claim.Value);
        }
    }
}
