using TiendaOnline.Application.Common;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Application.Usuarios.Queries
{
    public interface IUsuarioQueryService
    {
        Task<UsuarioPerfilDto?> ObtenerPerfil(int usuarioId, CancellationToken ct = default);
        Task<UsuarioResumenDto?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default);
        Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo, CancellationToken ct = default);

        /// <summary>Repartidores activos para el select de asignación del panel.</summary>
        Task<List<RepartidorResumenDto>> ObtenerRepartidoresActivosAsync(CancellationToken ct = default);
    }
}
