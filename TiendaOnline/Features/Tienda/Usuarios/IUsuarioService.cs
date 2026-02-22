using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Features.Tienda.Usuarios
{
    public interface IUsuarioService
    {
        Task<UsuarioPerfilDto> ObtenerPerfil(int usuarioId);
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
        Task<UsuarioUpdateDto> ObtenerUsuarioParaEdicionAsync(int usuarioId);
        Task EditarUsuarioAsync(UsuarioUpdateDto dto);
    }
}