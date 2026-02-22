namespace TiendaOnline.Features.Admin.HomeAdmin
{
    public interface IHomeAdminService
    {
        Task<IndexDTO> ObtenerResumenHomeAsync();
        Task<bool> VerificarEstadoBaseDatosAsync();
        string ObtenerVersionApp();
    }
}
