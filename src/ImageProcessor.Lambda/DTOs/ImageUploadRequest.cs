namespace ImageProcessor.Lambda.DTOs;

public sealed record ImageUploadRequest(string FileName, string ContentType, string Base64Image);