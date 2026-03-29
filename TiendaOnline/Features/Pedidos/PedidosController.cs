using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;
using TiendaOnline.Application.Usuarios.Common;
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

        public PedidosController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService, ICarritoService carritoService, IDireccionService direccionService, IPaymentService paymentService)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
            _carritoService = carritoService;
            _direccionService = direccionService;
            _paymentService = paymentService;
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

            var pedidos = await _pedidoQueryService.ObtenerPedidosDeUsuarioAsync(usuarioId);

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
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(id);
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

                EsAdmin = User.IsInRole("Administrador"),
                EsRepartidor = User.IsInRole("Repartidor"),
                EsPropioPedido = pedido.UsuarioId.ToString() ==
                                 User.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> CheckOut()
        {
            var items = await _carritoService.ObtenerAsync();

            if (items == null || items.Count == 0)
            {
                TempData["MensajeError"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Carritos");
            }

            var subtotal = items.Sum(i => i.Precio * i.Cantidad);

            var direcciones = await _direccionService.ObtenerDireccionesAsync(ObtenerUsuarioId());

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
                    Cantidad = i.Cantidad,
                    ImagenUrl = i.ImagenUrl
                }).ToList(),
                SubTotal = subtotal,
                CostoEnvio = 0
            };

            return View(viewModel);
        }

        [HttpPost]
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
                var dir = await _direccionService.ObtenerDireccionesAsync(ObtenerUsuarioId());
                model.DireccionesGuardadas = dir.Select(d => new DireccionGuardadaViewModel
                {
                    DireccionId = d.DireccionId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero
                }).ToList();

                return View("CheckOut", model);
            }

            return RedirectToAction("Confirmacion", model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Confirmacion(CheckOutViewModel model)
        {
            if (model == null) return RedirectToAction("CheckOut");

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
                    var dir = await _direccionService.ObtenerPorIdAsync(model.DireccionSeleccionadaId);
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

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarCompra(ConfirmacionPedidoViewModel model, MetodoPagoId metodoDePagoId)
        {
            var usuarioId = ObtenerUsuarioId();

            var carritoItems = await _carritoService.ObtenerAsync();

            if (model == null)
            {
                TempData["MensajeError"] = "La sesión de compra expiró o los datos son inválidos.";
                return RedirectToAction("CheckOut");
            }

            if (model == null || model.Direccion == null || model.Items == null)
            {
                TempData["MensajeError"] = "La sesión de compra expiró o los datos son inválidos.";
                return RedirectToAction("CheckOut");
            }

            var dto = new CrearPedidoDto
            {
                UsuarioId = usuarioId,
                MetodoDePagoId = (int)metodoDePagoId,
                EsEnvioADomicilio = model.MetodoEntrega == MetodoEntrega.EnvioDomicilio,
                EnvioCalle = model.Direccion.Calle,
                EnvioNumero = model.Direccion.Numero,
                EnvioPiso = model.Direccion.Piso,
                EnvioDepartamento = model.Direccion.Departamento,
                EnvioObservaciones = model.Direccion.Observaciones,
                EnvioLocalidad = model.Direccion.Localidad,
                EnvioProvincia = model.Direccion.Provincia,
                EnvioCodigoPostal = model.Direccion.CodigoPostal,
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
                var pedido = await _pedidoCommandService.CrearPedidoYPrepararPagoAsync(dto);

                if (model.Direccion.EsNueva == true && model.MetodoEntrega == MetodoEntrega.EnvioDomicilio)
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
                    });
                }

                // Si el método es Mercado Pago (ID 1 por ejemplo), generamos link
                if (metodoDePagoId == MetodoPagoId.MercadoPago)
                {
                    var urlPago = await _paymentService.GenerarPreferenciaPagoAsync(pedido);
                    return Redirect(urlPago);
                }

                // Si es otro método (ej. Efectivo), vamos directo a detalles
                await _carritoService.VaciarAsync();
                TempData["MensajeExito"] = "¡Pedido realizado con éxito!";
                return RedirectToAction("Detalles", new { id = pedido.PedidoId });
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction("Index", "Carritos");
            }
        }

        [Authorize(Policy = "EsDuenioDelPedido")]
        [HttpGet("[action]")]
        public IActionResult PagoExitoso([FromQuery] string external_reference, [FromQuery] string payment_id)
        {
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
        public async Task<IActionResult> ReintentarPago(int pedidoId)
        {
            // Buscamos el pedido existente con sus datos
            var pedidoDto = await _pedidoCommandService.ObtenerDatosParaPagoAsync(pedidoId);

            if (pedidoDto == null) return NotFound();

            // Generamos un nuevo link de Mercado Pago
            var urlPago = await _paymentService.GenerarPreferenciaPagoAsync(pedidoDto);

            // Mandamos al usuario a pagar otra vez
            return Redirect(urlPago);
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
