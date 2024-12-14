using CloudinaryDotNet.Actions;

namespace Auth.API.Services.ImageHandler
{
    public interface IImageService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file);
        Task<DeletionResult> DeleteImageAsync(string publicId);
    }
}
