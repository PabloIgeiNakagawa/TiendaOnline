using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Infrastructure.ExternalServices
{
    public class CloudinaryService : IImagenService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var acc = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> SubirImagenAsync(Stream archivoStream, string nombreArchivo)
        {
            if (archivoStream == null || archivoStream.Length == 0) return null;

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(nombreArchivo, archivoStream),
                Folder = "TechStore",
                Transformation = new Transformation()
                    .Width(800).Height(800).Crop("limit")
                    .Quality("auto").FetchFormat("auto")
            };

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

