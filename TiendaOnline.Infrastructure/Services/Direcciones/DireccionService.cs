using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Usuarios.Common;
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
            var direccion = await _context.Direcciones
                .Where(d => d.UsuarioId == id)
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

            return direccion ?? throw new InvalidOperationException("No se encontró la dirección para el usuario especificado.");
        }
    }
}
