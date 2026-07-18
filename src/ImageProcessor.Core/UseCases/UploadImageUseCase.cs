using ImageProcessor.Core.Interfaces;
using ImageProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ImageProcessor.Core.UseCases;

public sealed partial class UploadImageUseCase(
    IStorageService storage,
    IMetadataRepository repository,
    ILogger<UploadImageUseCase> logger)
{
    public async Task<ImageMetadata> ExecuteAsync(string base64Image, string fileName, string contentType)
    {
        try
        {
            LogUploadProcessStarted(logger, fileName);

            string s3Url = await storage.UploadBase64ImageAsync(base64Image, fileName, contentType);
            int sizeInBytes = Convert.FromBase64String(base64Image).Length;

            ImageMetadata metadata = new(Guid.NewGuid().ToString(), fileName, sizeInBytes, s3Url, DateTime.UtcNow);
            await repository.SaveMetadataAsync(metadata);
            LogMetadataPersisted(logger, metadata.ImageId, fileName);

            return metadata;
        }
        catch (Exception ex)
        {
            LogUploadProcessFailed(logger, ex, fileName);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Initiating image upload process for file: {FileName}.")]
    static partial void LogUploadProcessStarted(ILogger<UploadImageUseCase> logger, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Metadata {ImageId} successfully persisted for file {FileName}.")]
    static partial void LogMetadataPersisted(ILogger<UploadImageUseCase> logger, string imageId, string fileName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Critical error during image upload process for file {FileName}.")]
    static partial void LogUploadProcessFailed(ILogger<UploadImageUseCase> logger, Exception ex, string fileName);
}