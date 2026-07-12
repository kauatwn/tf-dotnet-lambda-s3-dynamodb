using ImageProcessor.Lambda.DTOs;
using ImageProcessor.Lambda.Models;

namespace ImageProcessor.Lambda.Core;

public sealed class ProcessImageUseCase(IStorage storage, IImageRepository repository)
{
    public async Task<ImageMetadata> ExecuteAsync(ImageUploadRequest request)
    {
        string s3Url = await storage.UploadBase64ImageAsync(request.Base64Image, request.FileName, request.ContentType);

        ImageMetadata metadata = new(
            Guid.NewGuid().ToString(),
            request.FileName,
            Convert.FromBase64String(request.Base64Image).Length,
            s3Url,
            DateTime.UtcNow);

        await repository.SaveMetadataAsync(metadata);

        return metadata;
    }
}