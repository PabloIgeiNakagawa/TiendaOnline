using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Carritos;

namespace TiendaOnline.ViewComponents
{
    public class CarritoHeaderViewComponent : ViewComponent
    {
        private readonly ICarritoService _carritoService;

        public CarritoHeaderViewComponent(ICarritoService carritoService)
        {
            _carritoService = carritoService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cantidad = await _carritoService.ObtenerCantidadTotalAsync();
            return View("CarritoHeader", cantidad);
        }
    }
}