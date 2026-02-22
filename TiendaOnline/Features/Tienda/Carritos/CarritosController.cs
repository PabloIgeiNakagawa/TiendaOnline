using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Helpers;
using TiendaOnline.Services.DTOs;
using TiendaOnline.Services.IServices;

namespace TiendaOnline.Features.Tienda.Carritos
{
    [Route("Carrito")]
    public class CarritosController : Controller
    {
        private const string CarritoKey = "Carrito";
        private readonly IProductoService _productoService;

        public CarritosController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        [HttpGet("[action]")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Carrito de Compras";
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>(CarritoKey) ?? new List<ItemCarrito>();
            return View(carrito);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int id)
        {
            var producto = await _productoService.ObtenerProductoAsync(id);
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>(CarritoKey) ?? new List<ItemCarrito>();

            var itemExistente = carrito.FirstOrDefault(x => x.ProductoId == id);
            if (itemExistente != null)
            {
                itemExistente.Cantidad++;
            }
            else
            {
                carrito.Add(new ItemCarrito
                {
                    ProductoId = producto.ProductoId,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = 1,
                    ImagenUrl = producto.ImagenUrl
                });
            }
            HttpContext.Session.SetObject(CarritoKey, carrito);
            TempData["MensajeExito"] = "Producto agregado al carrito.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarItem(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>(CarritoKey);
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(p => p.ProductoId == id);
                if (item != null)
                {
                    carrito.Remove(item);
                    HttpContext.Session.SetObject(CarritoKey, carrito);
                }
            }
            TempData["MensajeExito"] = "Producto eliminado del carrito.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCantidad(int productoId, int cantidad)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();

            var item = carrito.FirstOrDefault(p => p.ProductoId == productoId);
            if (item != null)
            {
                item.Cantidad = cantidad;
            }

            HttpContext.Session.SetObject(CarritoKey, carrito);
            TempData["MensajeExito"] = "Cantidad actualizada.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove("Carrito");
            TempData["MensajeExito"] = "Se ha vacíado el carrito.";
            return RedirectToAction(nameof(Index));
        }
    }

}
