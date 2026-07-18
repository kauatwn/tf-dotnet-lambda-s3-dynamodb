namespace ImageProcessor.Core.Models;

public sealed record ImageMetadata(
    string ImageId,
    string FileName, 
    long SizeInBytes, 
    string S3Url, 
    DateTime UploadDate);