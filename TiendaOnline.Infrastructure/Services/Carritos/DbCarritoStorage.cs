using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Carritos
{
    /// <summary>
    /// Storage del carrito del usuario logueado: una fila en CarritosUsuarios con el
    /// snapshot JSON de los items. Sobrevive la expiración de sesión y el cierre del
    /// browser; nombre/precio/imagen son snapshot de display y lo relevante
    /// (ProductoId y Cantidad) lo refresca ValidarStockAsync contra BD en cada lectura.
    /// </summary>
    public class DbCarritoStorage : ICarritoStorage
    {
        private readonly TiendaContext _context;
        private readonly IUserSessionService _userSession;

        public DbCarritoStorage(TiendaContext context, IUserSessionService userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<List<ItemCarrito>> ObtenerAsync()
        {
            var fila = await _context.CarritosUsuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UsuarioId == UsuarioIdActual());

            return CarritoSnapshotJson.Deserializar(fila?.Contenido);
        }

        public Task GuardarAsync(List<ItemCarrito> items)
            => GuardarSnapshotAsync(_context, UsuarioIdActual(), CarritoSnapshotJson.Serializar(items));

        public async Task LimpiarAsync()
        {
            var fila = await _context.CarritosUsuarios
                .FirstOrDefaultAsync(c => c.UsuarioId == UsuarioIdActual());

            if (fila != null)
            {
                _context.CarritosUsuarios.Remove(fila);
                await _context.SaveChangesAsync();
            }
        }

        // Upsert compartido con la fusión de sesión al loguear: misma fila, un solo save.
        internal static async Task GuardarSnapshotAsync(TiendaContext context, int usuarioId, string json)
        {
            var fila = await context.CarritosUsuarios.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (fila == null)
            {
                context.CarritosUsuarios.Add(new CarritoUsuario
                {
                    UsuarioId = usuarioId,
                    Contenido = json,
                    FechaActualizacion = DateTime.Now
                });
            }
            else
            {
                fila.Contenido = json;
                fila.FechaActualizacion = DateTime.Now;
            }

            await context.SaveChangesAsync();
        }

        private int UsuarioIdActual()
        {
            var userId = _userSession.GetUserId();
            if (userId is null)
            {
                throw new InvalidOperationException("El carrito en BD requiere un usuario logueado.");
            }

            return userId.Value;
        }
    }
}
