using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using TiendaOnline.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiendaOnline.Application.Common.Settings;

namespace TiendaOnline.Infrastructure.ExternalServices
{
    public class CloudinaryService : IImagenService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IOptions<CloudinarySettings> config, ILogger<CloudinaryService> logger)
        {
            _logger = logger;
            var settings = config.Value;
            var acc = new Account(
                settings.CloudName,
                settings.ApiKey,
                settings.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo, string subCarpeta = "General", int width = 800, int height = 800, CancellationToken ct = default)
        {
            if (archivoStream == null || archivoStream.Length == 0)
            {
                _logger.LogDebug("SubirImagenAsync recibió un stream vacío para {NombreArchivo}", nombreArchivo);
                return null;
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(nombreArchivo, archivoStream),
                // Carpeta dinámica: TechStore/Productos o TechStore/Branding
                Folder = $"TechStore/{subCarpeta}",

                Transformation = new Transformation()
                    .Width(width)
                    .Height(height)
                    .Crop("limit")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            if (subCarpeta == "Branding")
            {
                uploadParams.AccessControl = new List<AccessControlRule> {
                    new AccessControlRule { AccessType = AccessType.Anonymous }
                };
            }

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary rechazó la subida de {NombreArchivo}: {Error}", nombreArchivo, uploadResult.Error.Message);
                throw new Exception(uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }

        // Los comprobantes se suben sin transformación: la resolución original es la evidencia del pago.
        public async Task<string> SubirComprobanteAsync(Stream archivoStream, string nombreArchivo, CancellationToken ct = default)
        {
            if (archivoStream == null || archivoStream.Length == 0)
            {
                _logger.LogDebug("SubirComprobanteAsync recibió un stream vacío para {NombreArchivo}", nombreArchivo);
                return null;
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(nombreArchivo, archivoStream),
                Folder = "TechStore/Comprobantes"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary rechazó el comprobante {NombreArchivo}: {Error}", nombreArchivo, uploadResult.Error.Message);
                throw new Exception("No se pudo subir el comprobante. Intentá de nuevo en unos minutos.");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> BorrarImagenAsync(string publicId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result != "ok")
            {
                _logger.LogWarning("Cloudinary no pudo borrar la imagen {PublicId} (resultado: {Resultado})", publicId, result.Result);
            }

            return result.Result == "ok";
        }

        public string ExtraerPublicIdDesdeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            // Las URLs de Cloudinary tienen este formato: 
            // https://res.cloudinary.com/demo/image/upload/v12345/carpeta/nombre_imagen.jpg

            try
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(segments, "upload");

                var publicIdWithExtension = string.Join("/", segments.Skip(uploadIndex + 2));

                return publicIdWithExtension.Split('.')[0];
            }
            catch
            {
                return string.Empty;
            }
        }

    }
}

