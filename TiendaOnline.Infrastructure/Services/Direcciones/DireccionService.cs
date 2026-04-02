using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Usuarios.Common;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Direcciones
{
    public class DireccionService : IDireccionService
    {
        private readonly TiendaContext _context;

        public DireccionService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<List<DireccionDto>> ObtenerDireccionesAsync(int usuarioId)
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
                .ToListAsync();
        }

        public async Task<DireccionDto> ObtenerPorIdAsync(int? id)
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
                .FirstOrDefaultAsync();

            return direccion ?? throw new InvalidOperationException($"No se encontró la dirección con ID {id}.");
        }

        public async Task<int> GuardarDireccionAsync(int usuarioId, DireccionDto direccion)
        {
            if (direccion.EsPrincipal)
            {
                var direcciones = await _context.Direcciones
                    .Where(d => d.UsuarioId == usuarioId && d.Activo)
                    .ToListAsync();
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
            await _context.SaveChangesAsync();

            return nuevaDireccion.DireccionId;
        }

        public async Task ActualizarDireccionAsync(int direccionId, DireccionDto direccion)
        {
            var direccionExistente = await _context.Direcciones.FindAsync(direccionId);
            if (direccionExistente == null)
                throw new InvalidOperationException($"No se encontró la dirección con ID {direccionId}.");

            if (direccion.EsPrincipal && !direccionExistente.EsPrincipal)
            {
                var direcciones = await _context.Direcciones
                    .Where(d => d.UsuarioId == direccionExistente.UsuarioId && d.Activo && d.DireccionId != direccionId)
                    .ToListAsync();
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

            await _context.SaveChangesAsync();
        }

        public async Task EliminarDireccionAsync(int direccionId)
        {
            var direccion = await _context.Direcciones.FindAsync(direccionId);
            if (direccion == null)
                throw new InvalidOperationException($"No se encontró la dirección con ID {direccionId}.");

            direccion.Activo = false;
            await _context.SaveChangesAsync();
        }
    }
}
