using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Direcciones
{
    public class DireccionService : IDireccionService
    {
        private readonly TiendaContext _context;
        private readonly ILogger<DireccionService> _logger;

        public DireccionService(TiendaContext context, ILogger<DireccionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DireccionDto>> ObtenerDireccionesAsync(int usuarioId, CancellationToken ct = default)
        {
            return await _context.Direcciones
                .Where(d => d.UsuarioId == usuarioId && d.Activo)
                .Select(d => new DireccionDto
                {
                    DireccionId = d.DireccionId,
                    UsuarioId = d.UsuarioId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero,
                    Localidad = d.Localidad,
                    Provincia = d.Provincia,
                    CodigoPostal = d.CodigoPostal,
                    Piso = d.Piso,
                    Departamento = d.Departamento,
                    Observaciones = d.Observaciones,
                    EsPrincipal = d.EsPrincipal,
                    Activo = d.Activo
                })
                .ToListAsync(ct);
        }

        public async Task<DireccionDto> ObtenerPorIdAsync(int? id, CancellationToken ct = default)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var direccion = await _context.Direcciones
                .Where(d => d.DireccionId == id)
                .Select(d => new DireccionDto
                {
                    DireccionId = d.DireccionId,
                    UsuarioId = d.UsuarioId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero,
                    Provincia = d.Provincia,
                    Localidad = d.Localidad,
                    CodigoPostal = d.CodigoPostal,
                    Piso = d.Piso,
                    Departamento = d.Departamento,
                    Observaciones = d.Observaciones,
                    EsPrincipal = d.EsPrincipal,
                    Activo = d.Activo
                })
                .FirstOrDefaultAsync(ct);

            return direccion ?? throw new InvalidOperationException($"No se encontró la dirección con ID {id}.");
        }

        /// <summary>
        /// Lee una dirección SOLO si pertenece al usuario indicado; null si no existe
        /// o es ajena. Guard de ownership server-side para los flows donde el id
        /// llega del form (checkout): un DireccionSeleccionadaId ajeno nunca expone
        /// calle/observaciones de otros usuarios.
        /// </summary>
        public async Task<DireccionDto?> ObtenerDelUsuarioAsync(int direccionId, int usuarioId, CancellationToken ct = default)
        {
            return await _context.Direcciones
                .Where(d => d.DireccionId == direccionId && d.UsuarioId == usuarioId)
                .Select(d => new DireccionDto
                {
                    DireccionId = d.DireccionId,
                    UsuarioId = d.UsuarioId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero,
                    Localidad = d.Localidad,
                    Provincia = d.Provincia,
                    CodigoPostal = d.CodigoPostal,
                    Piso = d.Piso,
                    Departamento = d.Departamento,
                    Observaciones = d.Observaciones,
                    EsPrincipal = d.EsPrincipal,
                    Activo = d.Activo
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<int> GuardarDireccionAsync(int usuarioId, DireccionDto direccion, CancellationToken ct = default)
        {
            if (direccion.EsPrincipal)
            {
                var direcciones = await _context.Direcciones
                    .Where(d => d.UsuarioId == usuarioId && d.Activo)
                    .ToListAsync(ct);
                foreach (var d in direcciones)
                {
                    d.EsPrincipal = false;
                }
            }

            var nuevaDireccion = new Direccion
            {
                UsuarioId = usuarioId,
                Etiqueta = direccion.Etiqueta,
                Calle = direccion.Calle,
                Numero = direccion.Numero,
                Localidad = direccion.Localidad,
                Provincia = direccion.Provincia,
                CodigoPostal = direccion.CodigoPostal,
                Piso = direccion.Piso,
                Departamento = direccion.Departamento,
                Observaciones = direccion.Observaciones,
                EsPrincipal = direccion.EsPrincipal,
                Activo = true
            };

            _context.Direcciones.Add(nuevaDireccion);
            await _context.SaveChangesAsync(ct);
            // Solo ids: el contenido (calle, CP) es PII que la auditoría del pedido ya conserva.
            _logger.LogInformation("Nueva dirección {DireccionId} para usuario {UsuarioId}", nuevaDireccion.DireccionId, usuarioId);

            return nuevaDireccion.DireccionId;
        }

        public async Task ActualizarDireccionAsync(int direccionId, DireccionDto direccion, CancellationToken ct = default)
        {
            var direccionExistente = await _context.Direcciones.FindAsync(direccionId, ct);
            if (direccionExistente == null)
                throw new InvalidOperationException($"No se encontró la dirección con ID {direccionId}.");

            if (direccion.EsPrincipal && !direccionExistente.EsPrincipal)
            {
                var direcciones = await _context.Direcciones
                    .Where(d => d.UsuarioId == direccionExistente.UsuarioId && d.Activo && d.DireccionId != direccionId)
                    .ToListAsync(ct);
                foreach (var d in direcciones)
                {
                    d.EsPrincipal = false;
                }
            }

            direccionExistente.Etiqueta = direccion.Etiqueta;
            direccionExistente.Calle = direccion.Calle;
            direccionExistente.Numero = direccion.Numero;
            direccionExistente.Localidad = direccion.Localidad;
            direccionExistente.Provincia = direccion.Provincia;
            direccionExistente.CodigoPostal = direccion.CodigoPostal;
            direccionExistente.Piso = direccion.Piso;
            direccionExistente.Departamento = direccion.Departamento;
            direccionExistente.Observaciones = direccion.Observaciones;
            direccionExistente.EsPrincipal = direccion.EsPrincipal;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Dirección {DireccionId} (usuario {UsuarioId}) actualizada", direccionId, direccionExistente.UsuarioId);
        }

        public async Task EliminarDireccionAsync(int direccionId, CancellationToken ct = default)
        {
            var direccion = await _context.Direcciones.FindAsync(direccionId, ct);
            if (direccion == null)
                throw new InvalidOperationException($"No se encontró la dirección con ID {direccionId}.");

            direccion.Activo = false;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Dirección {DireccionId} (usuario {UsuarioId}) eliminada", direccionId, direccion.UsuarioId);
        }
    }
}
