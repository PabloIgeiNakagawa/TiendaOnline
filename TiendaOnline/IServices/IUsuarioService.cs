using TiendaOnline.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TiendaOnline.Services
{
    public interface IUsuarioService
    {
        Task CrearUsuarioAsync(Usuario usuario);
        Task DarAltaUsuarioAsync(int usuarioId);
        Task DarBajaUsuarioAsync(int usuarioId);
        Task EditarUsuarioAsync(int id, Usuario usuarioEditado);
        Task<Usuario?> ObtenerPorEmailAsync(string email);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
    }
}