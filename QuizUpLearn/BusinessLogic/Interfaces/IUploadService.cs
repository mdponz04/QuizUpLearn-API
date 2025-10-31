using Microsoft.AspNetCore.Http;

namespace BusinessLogic.Interfaces
{
    public interface IUploadService
    {
        Task<(string Url, string PublicId)> UploadAsync(IFormFile file, string? publicId = null);
        Task<bool> DeleteFileAsync(string fileUrl, string? publicId = null);
        Task<IFormFile> ConvertByteArrayToIFormFile(byte[] fileBytes, string fileName, string contentType);
    }
}
