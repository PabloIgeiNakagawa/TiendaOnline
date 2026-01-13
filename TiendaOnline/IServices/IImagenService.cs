namespace TiendaOnline.IServices
{
    public interface IImagenService
    {
        Task<string> SubirImagenAsync(IFormFile archivo);
        Task<bool> BorrarImagenAsync(string publicId);
        string ExtraerPublicIdDesdeUrl(string url);
    }
}
