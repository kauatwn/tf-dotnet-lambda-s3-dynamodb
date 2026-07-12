using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessor.Lambda.Core;

namespace ImageProcessor.Lambda.Infrastructure;

public sealed class S3Storage(IAmazonS3 s3Client) : IStorage
{
    private readonly string _bucketName = Environment.GetEnvironmentVariable("BUCKET_NAME") ??
                                          throw new ArgumentNullException("BUCKET_NAME não configurado.");

    public async Task<string> UploadBase64ImageAsync(string base64Image, string fileName, string contentType)
    {
        byte[] imageBytes = Convert.FromBase64String(base64Image);
        string uniqueFileName = $"{Guid.NewGuid()}-{fileName}";

        using MemoryStream memoryStream = new(imageBytes);

        PutObjectRequest request = new()
        {
            BucketName = _bucketName,
            Key = uniqueFileName,
            InputStream = memoryStream,
            ContentType = contentType
        };

        await s3Client.PutObjectAsync(request);

        return $"s3://{_bucketName}/{uniqueFileName}";
    }
}