using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Infrastructure.Services.Usuarios
{
    public class RolQueryService : IRolQueryService
    {
        // Aquí puedes usar el Enum que está en TiendaOnline.Domain.Entities
        public Task<List<RolDto>> ObtenerTodosAsync()
        {
            return Task.FromResult(Enum.GetValues(typeof(Rol))
                .Cast<Rol>()
                .Select(r => new RolDto
                {
                    Id = (int)r,
                    Nombre = r.ToString()
                }).ToList());
        }
    }
}
