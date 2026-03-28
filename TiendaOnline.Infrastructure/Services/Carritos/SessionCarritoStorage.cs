using Microsoft.AspNetCore.Http;
using TiendaOnline.Application.Carritos;

namespace TiendaOnline.Infrastructure.Services.Carritos
{
    public class SessionCarritoStorage : ICarritoStorage
    {
        private const string CarritoKey = "Carrito";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionCarritoStorage(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<List<ItemCarrito>> ObtenerAsync()
        {
            var session = _httpContextAccessor.HttpContext!.Session;
            var carrito = session.GetObject<List<ItemCarrito>>(CarritoKey)
                          ?? new List<ItemCarrito>();

            return Task.FromResult(carrito);
        }

        public Task GuardarAsync(List<ItemCarrito> items)
        {
            var session = _httpContextAccessor.HttpContext!.Session;
            session.SetObject(CarritoKey, items);
            return Task.CompletedTask;
        }

        public Task LimpiarAsync()
        {
            var session = _httpContextAccessor.HttpContext!.Session;
            session.Remove(CarritoKey);
            return Task.CompletedTask;
        }
    }
}
