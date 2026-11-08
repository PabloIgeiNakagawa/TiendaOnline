using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.E2E.Host
{
    /// <summary>
    /// Imagenes de mentira: no habla con Cloudinary. Devuelve una URL fijada para
    /// que las vistas muestren algo y registra las bajas pedidas; los smoke tests v1
    /// suben productos solo por seed directo a la DB.
    /// </summary>
    public class E2EImagenFalso : IImagenService
    {
        public List<string> Borradas { get; } = new();

        public Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo, string subCarpeta = "General", int width = 800, int height = 800, CancellationToken ct = default)
            => Task.FromResult($"https://img.e2e.test/{subCarpeta}/{nombreArchivo}");

        public Task<string> SubirComprobanteAsync(Stream archivoStream, string nombreArchivo, CancellationToken ct = default)
            => Task.FromResult($"https://img.e2e.test/comprobantes/{nombreArchivo}");

        public Task<bool> BorrarImagenAsync(string publicId, CancellationToken ct = default)
        {
            Borradas.Add(publicId);
            return Task.FromResult(true);
        }

        public string ExtraerPublicIdDesdeUrl(string url) => url.TrimEnd('/').Split('/').Last();
    }
}
