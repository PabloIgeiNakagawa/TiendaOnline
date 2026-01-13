using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using TiendaOnline.IServices;

namespace TiendaOnline.Services
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

        public async Task<string> SubirImagenAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0) return null;

            using var stream = archivo.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(archivo.FileName, stream),
                // Esto optimiza la imagen al subirla para no gastar créditos de más
                Transformation = new Transformation()
                    .Width(800).Height(800).Crop("limit")
                    .Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

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
