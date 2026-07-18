using ImageProcessor.Core.Models;

namespace ImageProcessor.Core.Interfaces;

public interface IMetadataRepository
{
    Task SaveMetadataAsync(ImageMetadata metadata);
}