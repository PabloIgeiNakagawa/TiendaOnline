namespace TiendaOnline.Application.Common.Interfaces
{
    public interface IImagenService
    {
        Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo, string subCarpeta = "General", int width = 800, int height = 800);
        Task<bool> BorrarImagenAsync(string publicId);
        string ExtraerPublicIdDesdeUrl(string url);
    }
}
