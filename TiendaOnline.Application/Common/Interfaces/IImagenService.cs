namespace TiendaOnline.Application.Common.Interfaces
{
    public interface IImagenService
    {
        Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo);
        Task<bool> BorrarImagenAsync(string publicId);
        string ExtraerPublicIdDesdeUrl(string url);
    }
}
