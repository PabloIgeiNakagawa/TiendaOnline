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
                .Where(d => d.UsuarioId == usuarioId)
                .Select(d => new DireccionDto
                {
                    DireccionId = d.DireccionId,
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero,
                    Localidad = d.Localidad,
                    Provincia = d.Provincia,
                    CodigoPostal = d.CodigoPostal,
                    Observaciones = d.Observaciones
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
                    Etiqueta = d.Etiqueta,
                    Calle = d.Calle,
                    Numero = d.Numero,
                    Provincia = d.Provincia,
                    Localidad = d.Localidad,
                    CodigoPostal = d.CodigoPostal,
                    Piso = d.Piso,
                    Departamento = d.Departamento,
                    Observaciones = d.Observaciones
                })
                .FirstOrDefaultAsync();

            return direccion ?? throw new InvalidOperationException($"No se encontró la dirección con ID {id}.");
        }

        public async Task<int> GuardarDireccionAsync(int usuarioId, DireccionDto direccion)
        {
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
                Observaciones = direccion.Observaciones
            };

            _context.Direcciones.Add(nuevaDireccion);
            await _context.SaveChangesAsync();

            return nuevaDireccion.DireccionId;
        }
    }
}
