using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaOnline.Application.Carritos;

namespace TiendaOnline.Features.Carritos
{
    [Route("Carrito")]
    public class CarritosController : Controller
    {
        private readonly ICarritoService _carritoService;

        public CarritosController(ICarritoService carritoService)
        {
            _carritoService = carritoService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Index()
        {
            var validacion = await _carritoService.ValidarStockAsync();
            if (validacion.IsFailure)
            {
                TempData["MensajeError"] = validacion.Error;
            }

            if (validacion.IsSuccess && !validacion.Value.TodoOK)
            {
                foreach (var item in validacion.Value.ItemsSinStock)
                {
                    TempData["MensajeError"] = $"Stock insuficiente para {item.Nombre}: solicitaste {item.CantidadSolicitada} pero hay {item.StockDisponible} disponibles.";
                }
            }

            var carrito = await _carritoService.ObtenerAsync();
            return View(carrito);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("carrito")]
        public async Task<IActionResult> Agregar(int id)
        {
            if (id <= 0)
                return BadRequest();

            var resultado = await _carritoService.AgregarAsync(id);

            if (resultado.IsFailure)
            {
                TempData["MensajeError"] = resultado.Error;
            }
            else
            {
                TempData["MensajeExito"] = "Producto agregado al carrito.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarItem(int id)
        {
            var resultado = await _carritoService.EliminarAsync(id);

            if (resultado.IsFailure)
            {
                TempData["MensajeError"] = resultado.Error;
            }
            else
            {
                TempData["MensajeExito"] = "Producto eliminado del carrito.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCantidad(int productoId, int cantidad)
        {
            if (cantidad <= 0)
            {
                var resultado = await _carritoService.EliminarAsync(productoId);
                TempData["MensajeExito"] = "Producto eliminado del carrito.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _carritoService.ActualizarCantidadAsync(productoId, cantidad);

            if (result.IsFailure)
            {
                TempData["MensajeError"] = result.Error;
            }
            else
            {
                TempData["MensajeExito"] = "Cantidad actualizada.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vaciar()
        {
            await _carritoService.VaciarAsync();
            TempData["MensajeExito"] = "Se ha vaciado el carrito.";
            return RedirectToAction(nameof(Index));
        }
    }
}
