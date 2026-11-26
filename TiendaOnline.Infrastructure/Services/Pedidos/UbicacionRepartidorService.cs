using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    /// <summary>
    /// Última posición del repartidor mientras comparte ubicación desde el
    /// teléfono. Una fila por repartidor que se pisa en cada latido: no hay
    /// historial de recorridos.
    /// </summary>
    public class UbicacionRepartidorService : IUbicacionRepartidorService
    {
        private readonly TiendaContext _context;
        private readonly IUserSessionService _userSessionService;

        public UbicacionRepartidorService(TiendaContext context, IUserSessionService userSessionService)
        {
            _context = context;
            _userSessionService = userSessionService;
        }

        public async Task RegistrarAsync(double latitud, double longitud, double? precisionMetros, CancellationToken ct = default)
        {
            if (double.IsNaN(latitud) || double.IsNaN(longitud)
                || latitud is < -90 or > 90
                || longitud is < -180 or > 180)
            {
                throw new UbicacionNoValidaException("La ubicación informada no es válida.");
            }

            var repartidorId = _userSessionService.GetUserId()
                ?? throw new UsuarioNoEncontradoException("No hay usuario en la sesión.");

            var ubicacion = await _context.UbicacionesRepartidor
                .FirstOrDefaultAsync(u => u.UsuarioId == repartidorId, ct);

            if (ubicacion == null)
            {
                _context.UbicacionesRepartidor.Add(new UbicacionRepartidor
                {
                    UsuarioId = repartidorId,
                    Latitud = latitud,
                    Longitud = longitud,
                    PrecisionMetros = precisionMetros,
                    ActualizadoEn = DateTime.Now
                });
            }
            else
            {
                ubicacion.Latitud = latitud;
                ubicacion.Longitud = longitud;
                ubicacion.PrecisionMetros = precisionMetros;
                ubicacion.ActualizadoEn = DateTime.Now;
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task DetenerAsync(CancellationToken ct = default)
        {
            var repartidorId = _userSessionService.GetUserId()
                ?? throw new UsuarioNoEncontradoException("No hay usuario en la sesión.");

            await _context.UbicacionesRepartidor
                .Where(u => u.UsuarioId == repartidorId)
                .ExecuteDeleteAsync(ct);
        }

        public async Task<bool> EstaCompartiendoAsync(CancellationToken ct = default)
        {
            var repartidorId = _userSessionService.GetUserId();
            if (repartidorId == null) return false;

            return await _context.UbicacionesRepartidor
                .AnyAsync(u => u.UsuarioId == repartidorId.Value, ct);
        }
    }
}
