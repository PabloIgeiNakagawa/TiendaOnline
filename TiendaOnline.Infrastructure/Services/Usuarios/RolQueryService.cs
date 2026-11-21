using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Infrastructure.Services.Usuarios
{
    public class RolQueryService : IRolQueryService
    {
        public Task<List<RolDto>> ObtenerTodosAsync(CancellationToken ct = default)
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
