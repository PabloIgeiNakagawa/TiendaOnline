using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TiendaOnline.Helpers;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Controllers
{
    public class CarritoController : Controller
    {
        private const string CarritoKey = "Carrito";
        private readonly IProductoService _productoService;

        public CarritoController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        public IActionResult Index()
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>(CarritoKey) ?? new List<ItemCarrito>();
            return View("~/Views/Pedido/Carrito.cshtml", carrito);
        }

        [HttpPost]
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
                    Imagen = producto.Imagen
                });
            }
            HttpContext.Session.SetObject(CarritoKey, carrito);
            TempData["MensajeExito"] = "Producto agregado al carrito correctamente.";
            return RedirectToAction("Index", "Carrito");
        }

        [HttpPost]
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
            TempData["MensajeExito"] = "Producto eliminado del carrito correctamente.";
            return RedirectToAction("Index", "Carrito");
        }

        [HttpPost]
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
            TempData["MensajeExito"] = "Cantidad actualizada correctamente.";
            return RedirectToAction("Index", "Carrito");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove("Carrito");
            TempData["MensajeExito"] = "Se ha vacíado el carrito.";
            return RedirectToAction("Index", "Carrito");
        }
    }

}
