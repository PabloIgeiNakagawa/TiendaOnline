namespace TiendaOnline.Application.Common.Interfaces
{
    public interface IImagenService
    {
        // ct: el SDK de Cloudinary no lo expone en Upload/Destroy; queda listo para cuando el SDK lo soporte.
        Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo, string subCarpeta = "General", int width = 800, int height = 800, CancellationToken ct = default);
        Task<string> SubirComprobanteAsync(Stream archivoStream, string nombreArchivo, CancellationToken ct = default);
        Task<bool> BorrarImagenAsync(string publicId, CancellationToken ct = default);
        string ExtraerPublicIdDesdeUrl(string url);
    }
}
