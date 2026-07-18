namespace ImageProcessor.Lambda.UploadImage.Contracts;

public sealed record UploadImageResponse(string ImageId, string S3Url, long SizeInBytes, DateTime UploadDate);