namespace ImageProcessor.Lambda.UploadImage.Contracts;

public sealed record UploadImageRequest(string FileName, string ContentType, string Base64Image);