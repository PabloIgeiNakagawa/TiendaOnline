using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Infrastructure.Services.Carritos
{
    /// <summary>
    /// El carrito reside donde toque según quién mira: usuario logueado → BD
    /// (sobrevive la expiración de sesión), anónimo → session. Sin lógica propia,
    /// delega en el storage concreto decidido por cada request.
    /// </summary>
    public class RoutingCarritoStorage : ICarritoStorage
    {
        private readonly IUserSessionService _userSession;
        private readonly SessionCarritoStorage _sesion;
        private readonly DbCarritoStorage _bd;

        public RoutingCarritoStorage(IUserSessionService userSession, SessionCarritoStorage sesion, DbCarritoStorage bd)
        {
            _userSession = userSession;
            _sesion = sesion;
            _bd = bd;
        }

        private ICarritoStorage Actual => _userSession.GetUserId() is null ? _sesion : _bd;

        public Task<List<ItemCarrito>> ObtenerAsync() => Actual.ObtenerAsync();

        public Task GuardarAsync(List<ItemCarrito> items) => Actual.GuardarAsync(items);

        public Task LimpiarAsync() => Actual.LimpiarAsync();
    }
}
