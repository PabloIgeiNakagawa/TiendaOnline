using TiendaOnline.Application.Usuarios.Common;

namespace TiendaOnline.Application.Direcciones
{
    public interface IDireccionService
    {
        Task<DireccionDto> ObtenerPorIdAsync(int? id);
        Task<List<DireccionDto>> ObtenerDireccionesAsync(int usuarioId);
        Task<int> GuardarDireccionAsync(int usuarioId, DireccionDto direccion);
    }
}
