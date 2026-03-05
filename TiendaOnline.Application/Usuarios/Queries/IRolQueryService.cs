namespace TiendaOnline.Application.Usuarios.Queries
{
    public interface IRolQueryService
    {
        Task<List<RolDto>> ObtenerTodosAsync();
    }
}
