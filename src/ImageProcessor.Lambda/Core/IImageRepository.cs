using ImageProcessor.Lambda.Models;

namespace ImageProcessor.Lambda.Core;

public interface IImageRepository
{
    Task SaveMetadataAsync(ImageMetadata metadata);
}