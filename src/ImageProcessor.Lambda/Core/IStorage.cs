namespace ImageProcessor.Lambda.Core;

public interface IStorage
{
    Task<string> UploadBase64ImageAsync(string base64Image, string fileName, string contentType);
}
