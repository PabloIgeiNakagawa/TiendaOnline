using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Carritos
{
    /// <summary>
    /// Fusión al loguear: el visitante venía armando un carrito anónimo (session) y
    /// al entrar con su cuenta esos items pasan a su carrito guardado en BD, sumando
    /// cantidades por producto. Un exceso de stock lo detecta ValidarStockAsync en el
    /// próximo GET del carrito y de ahí sale el aviso amable habitual.
    /// </summary>
    public class CarritoSesionMigrador : ICarritoSesionMigrador
    {
        private readonly SessionCarritoStorage _sesion;
        private readonly TiendaContext _context;

        public CarritoSesionMigrador(SessionCarritoStorage sesion, TiendaContext context)
        {
            _sesion = sesion;
            _context = context;
        }

        public async Task<bool> FusionarConCarritoGuardadoAsync(int usuarioId, CancellationToken ct = default)
        {
            var itemsDeSesion = (await _sesion.ObtenerAsync()).Where(i => i.Cantidad > 0).ToList();

            if (itemsDeSesion.Count == 0)
            {
                return false;
            }

            var fila = await _context.CarritosUsuarios
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

            var fusionados = fila is null
                ? new List<ItemCarrito>()
                : CarritoSnapshotJson.Deserializar(fila.Contenido);

            foreach (var item in itemsDeSesion)
            {
                var existente = fusionados.FirstOrDefault(x => x.ProductoId == item.ProductoId);
                if (existente == null)
                {
                    fusionados.Add(item);
                }
                else
                {
                    existente.Cantidad += item.Cantidad;
                }
            }

            await DbCarritoStorage.GuardarSnapshotAsync(_context, usuarioId, CarritoSnapshotJson.Serializar(fusionados));
            await _sesion.LimpiarAsync();

            return true;
        }
    }
}
