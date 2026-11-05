namespace TiendaOnline.Application.Usuarios.Queries
{
    public interface IRolQueryService
    {
        Task<List<RolDto>> ObtenerTodosAsync(CancellationToken ct = default);
    }
}
