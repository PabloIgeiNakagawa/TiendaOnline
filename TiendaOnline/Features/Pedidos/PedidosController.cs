using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;

namespace TiendaOnline.Features.Pedidos
{
    [Route("[controller]")]
    public class PedidosController : Controller
    {
        private readonly IPedidoQueryService _pedidoQueryService;
        private readonly IPedidoCommandService _pedidoCommandService;
        private readonly ICarritoService _carritoService;
        private readonly IDireccionService _direccionService;

        public PedidosController(IPedidoQueryService pedidoQueryService, IPedidoCommandService pedidoCommandService, ICarritoService carritoService, IDireccionService direccionService)
        {
            _pedidoQueryService = pedidoQueryService;
            _pedidoCommandService = pedidoCommandService;
            _carritoService = carritoService;
            _direccionService = direccionService;
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
                        Estado = p.EstadoNombre,
                        EstadoCss = ObtenerClaseEstado(p.EstadoId)
                    })
                    .ToList()
            };

            return View(viewmodel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Detalles(int id)
        {
            var pedido = await _pedidoQueryService.ObtenerPedidoConDetallesAsync(id);
            if (pedido == null)
                return NotFound();

            var subtotal = pedido.Items.Sum(d => d.Cantidad * d.PrecioUnitario);
            var iva = subtotal * 0.19m;

            var viewModel = new PedidoDetalleViewModel
            {
                PedidoId = pedido.PedidoId,
                FechaPedido = pedido.FechaPedido,
                FechaEnvio = pedido.FechaEnvio,
                FechaEntrega = pedido.FechaEntrega,
                FechaCancelado = pedido.FechaCancelado,
                Estado = pedido.EstadoNombre,

                UsuarioNombre = pedido.UsuarioNombre,
                UsuarioEmail = pedido.UsuarioEmail,
                UsuarioTelefono = pedido.UsuarioTelefono,

                Items = pedido.Items.Select(d => new PedidoItemViewModel
                {
                    ProductoNombre = d.ProductoNombre,
                    ProductoImagenUrl = d.ProductoImagenUrl,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList(),

                Subtotal = subtotal,
                IVA = iva,
                Total = subtotal + iva,

                NumeroSeguimiento = pedido.EstadoNombre == "Enviado"
                    ? $"TRK{pedido.PedidoId:D6}CO"
                    : null,

                FechaEstimadaEntrega = pedido.EstadoNombre == "Enviado"
                    ? pedido.FechaPedido.AddDays(7)
                    : null,

                EsAdmin = User.IsInRole("Administrador"),
                EsRepartidor = User.IsInRole("Repartidor"),
                EsPropioPedido = pedido.UsuarioId.ToString() ==
                                 User.FindFirstValue(ClaimTypes.NameIdentifier),

                PuedeCancelar = pedido.EstadoNombre == "Pendiente",
                PuedeEnviar = pedido.EstadoNombre == "Pendiente",
                PuedeEntregar = pedido.EstadoNombre == "Enviado"
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> CheckOut()
        {
            // Obtener ítems del carrito
            var items = await _carritoService.ObtenerAsync();

            if (items == null || !items.Any())
            {
                TempData["MensajeError"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Carritos");
            }

            var subtotal = items.Sum(i => i.Precio * i.Cantidad);

            // 2. Simular obtener direcciones guardadas del usuario (esto iría en un Service)
            var direcciones = new List<DireccionGuardadaViewModel>(); // await _usuarioService.ObtenerDireccionesAsync(userId);

            var nombre = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var telefono = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty;

            var viewModel = new CheckOutViewModel
            {
                Items = items.Select(i => new CheckOutItemViewModel
                {
                    ProductoId = i.ProductoId,
                    Nombre = i.Nombre,
                    Precio = i.Precio,
                    Cantidad = i.Cantidad,
                    ImagenUrl = i.ImagenUrl
                }).ToList(),
                SubTotal = subtotal,
                CostoEnvio = 0,
                Nombre = nombre,
                Email = email,
                Telefono = telefono
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarCheckout(CheckOutViewModel model)
        {
            // 1. RECARGAR DATOS DEL CARRITO (Vital para que no se pierdan los Items)
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
            // Asigná el costo de envío según tu lógica de negocio
            model.CostoEnvio = model.MetodoEntrega == "EnvioDomicilio" ? 500 : 0;

            // 2. Lógica de validación
            if (model.MetodoEntrega == "EnvioDomicilio")
            {
                if (model.DireccionSeleccionadaId == null && string.IsNullOrEmpty(model.NuevaDireccion?.Calle))
                {
                    ModelState.AddModelError("", "Debes seleccionar o ingresar una dirección.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Si hay errores, las direcciones guardadas también se pierden, hay que recargarlas
                var dir = await _direccionService.ObtenerDireccionesAsync(ObtenerUserId());
                model.DireccionesGuardadas = dir.Select(d => new DireccionGuardadaViewModel
                {
                    DireccionId = d.DireccionId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero
                }).ToList();

                return View("CheckOut", model);
            }

            // 3. Guardamos en TempData (Ahora con Items cargados)
            TempData["DatosPedido"] = JsonConvert.SerializeObject(model);

            return RedirectToAction("Confirmacion");
        }

        [HttpGet]
        public async Task<IActionResult> Confirmacion()
        {
            // Recuperamos de TempData
            var datosRaw = TempData.Peek("DatosPedido") as string; // Usamos Peek para que no se borre si refrescan
            if (string.IsNullOrEmpty(datosRaw)) return RedirectToAction("CheckOut");

            var checkoutData = JsonConvert.DeserializeObject<CheckOutViewModel>(datosRaw);

            var viewModel = new ConfirmacionPedidoViewModel
            {
                Items = checkoutData.Items.Select(i => new ItemCarrito
                {
                    ProductoId = i.ProductoId,
                    Nombre = i.Nombre,
                    Precio = i.Precio,
                    Cantidad = i.Cantidad,
                    ImagenUrl = i.ImagenUrl
                }).ToList(),
                SubTotal = checkoutData.SubTotal,
                CostoEnvio = checkoutData.CostoEnvio,
                MetodoEntrega = checkoutData.MetodoEntrega,
                NombreUsuario = checkoutData.Nombre,
                EmailUsuario = checkoutData.Email
            };

            // 3. Lógica para la dirección a mostrar
            if (checkoutData.MetodoEntrega == "EnvioDomicilio")
            {
                if (checkoutData.DireccionSeleccionadaId.HasValue)
                {
                    var dir = await _direccionService.ObtenerPorIdAsync(checkoutData.DireccionSeleccionadaId);
                    viewModel.Direccion = new DireccionCheckOut
                    {
                        Etiqueta = dir.Etiqueta,
                        Provincia = dir.Provincia,
                        Localidad = dir.Localidad,
                        CodigoPostal = dir.CodigoPostal,
                        Calle = dir.Calle,
                        Numero = dir.Numero,
                        Observaciones = dir.Observaciones ?? string.Empty
                    };
                }
                else
                {
                    var d = checkoutData.NuevaDireccion;
                    viewModel.Direccion = new DireccionCheckOut
                    {
                        Etiqueta = d.Etiqueta,
                        Provincia = d.Provincia,
                        Localidad = d.Localidad,
                        CodigoPostal = d.CodigoPostal,
                        Calle = d.Calle,
                        Numero = d.Numero,
                        Observaciones = d.Observaciones ?? string.Empty
                    };
                }
            }

            return View(viewModel);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarCompra()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                return Unauthorized();

            int usuarioId = int.Parse(claim.Value);

            var resultado = await _pedidoCommandService.CrearPedidoAsync(usuarioId);

            if (resultado < 0)
            {
                TempData["MensajeError"] = "No se ha podido crear el pedido.";
                return RedirectToAction("Index", "Carrito");
            }

            TempData["MensajeExito"] = "¡Pedido realizado con éxito!";
            return RedirectToAction("Detalles", new { id = resultado });
        }

        private string ObtenerClaseEstado(int estadoId)
        {
            return estadoId switch
            {
                0 => "text-bg-warning",  // Pendiente
                1 => "text-bg-primary",  // Enviado
                2 => "text-bg-success",  // Entregado
                3 => "text-bg-danger",   // Cancelado
                _ => "text-bg-secondary"
            };
        }
        private int ObtenerUserId()
        {
            // Buscamos el claim de tipo NameIdentifier (que es el ID en Identity)
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null) return 0; // O manejar el error si no está logueado

            return int.Parse(claim.Value);
        }
    }
}
