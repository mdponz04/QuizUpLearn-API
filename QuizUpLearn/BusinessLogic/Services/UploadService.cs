using BusinessLogic.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services
{
    public class UploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;

        public UploadService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:APIKey"];
            var apiSecret = configuration["Cloudinary:APISecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (string.Empty, string.Empty);

            var finalPublicId = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName).ToLower();

            await using var stream = file.OpenReadStream();
            
            if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp")
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "images",
                    PublicId = finalPublicId,
                    Overwrite = true,
                    Invalidate = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                var outputPublicId = "images/" + finalPublicId;
                return (uploadResult.SecureUrl?.ToString()!, outputPublicId);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "files",
                    PublicId = finalPublicId,
                    Overwrite = true,
                    Invalidate = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                var outputPublicId = "files/" + finalPublicId;
                return (uploadResult.SecureUrl?.ToString()!, outputPublicId);
            }
        }
        public async Task<bool> DeleteFileAsync(string? fileUrl = null)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return false;

            var uri = new Uri(fileUrl);
            var segments = uri.Segments;
            var folder = segments[^2].TrimEnd('/');
            var fileNameDecoded = Uri.UnescapeDataString(segments[^1]);

            string finalPublicId;
            ResourceType resourceType;

            if (folder == "files") // raw files
            {
                finalPublicId = $"{folder}/{fileNameDecoded}"; // keep extension
                resourceType = ResourceType.Raw;
            }
            else // images
            {
                finalPublicId = $"{folder}/{Path.GetFileNameWithoutExtension(fileNameDecoded)}";
                resourceType = ResourceType.Image;
            }

            var deletionParams = new DeletionParams(finalPublicId)
            {
                ResourceType = resourceType
            };

            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }

        public async Task<IFormFile> ConvertByteArrayToIFormFile(byte[] fileBytes, string fileName, string contentType)
        {
            var stream = new MemoryStream();
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return file;
        }
    }
}
