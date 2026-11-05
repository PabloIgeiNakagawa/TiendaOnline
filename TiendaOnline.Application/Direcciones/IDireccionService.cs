using TiendaOnline.Application.Usuarios.Common;

namespace TiendaOnline.Application.Direcciones
{
    public interface IDireccionService
    {
        Task<DireccionDto> ObtenerPorIdAsync(int? id, CancellationToken ct = default);
        Task<List<DireccionDto>> ObtenerDireccionesAsync(int usuarioId, CancellationToken ct = default);
        Task<int> GuardarDireccionAsync(int usuarioId, DireccionDto direccion, CancellationToken ct = default);
        Task ActualizarDireccionAsync(int direccionId, DireccionDto direccion, CancellationToken ct = default);
        Task EliminarDireccionAsync(int direccionId, CancellationToken ct = default);
    }
}
