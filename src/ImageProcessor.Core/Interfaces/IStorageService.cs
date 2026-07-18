namespace ImageProcessor.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadBase64ImageAsync(string base64Image, string fileName, string contentType);
}