using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using TiendaOnline.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using TiendaOnline.Application.Common.Settings;

namespace TiendaOnline.Infrastructure.ExternalServices
{
    public class CloudinaryService : IImagenService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var settings = config.Value;
            var acc = new Account(
                settings.CloudName,
                settings.ApiKey,
                settings.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo, string subCarpeta = "General", int width = 800, int height = 800)
        {
            if (archivoStream == null || archivoStream.Length == 0) return null;

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
                throw new Exception(uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> BorrarImagenAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

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

