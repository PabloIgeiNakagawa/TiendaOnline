using TiendaOnline.Domain.DTOs.Admin.Home;

namespace TiendaOnline.Services.IServices.Admin
{
    public interface IHomeService
    {
        Task<IndexDTO> ObtenerResumenHomeAsync();
        Task<bool> VerificarEstadoBaseDatosAsync();
        string ObtenerVersionApp();
    }
}
