using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ImageProcessor.Infrastructure.Storage;
using ImageProcessor.IntegrationTests.Abstractions;

namespace ImageProcessor.IntegrationTests.Infrastructure.Storage;

[Collection(nameof(IntegrationTestCollection))]
public class S3StorageServiceTests
{
    private readonly AmazonS3Client _s3Client;
    
    private readonly S3StorageService _sut;

    public S3StorageServiceTests(IntegrationTestFixture fixture)
    {
        // Creates the client pointing to the LocalStack Docker container
        _s3Client = new AmazonS3Client(new AmazonS3Config
        {
            ServiceURL = fixture.LocalStack.GetConnectionString(),
            ForcePathStyle = true
        });

        _sut = new S3StorageService(_s3Client);
    }

    [Fact(DisplayName = "UploadBase64ImageAsync should upload file correctly to S3 and return the expected S3 URI pattern")]
    public async Task UploadBase64ImageAsync_ShouldUploadCorrectly_AndReturnS3Url()
    {
        // Arrange
        const string validBase64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
        const string fileName = "unit-test-pic.png";
        const string contentType = "image/png";

        // Act
        string s3UrlResult = await _sut.UploadBase64ImageAsync(validBase64Image, fileName, contentType);

        // Assert 1: Validate if the returned URL pattern follows the business rules
        Assert.NotNull(s3UrlResult);
        Assert.StartsWith($"s3://{IntegrationTestFixture.TargetBucketName}/", s3UrlResult);

        // Extracts the dynamically generated Key to fetch directly from LocalStack
        AmazonS3Uri s3Uri = new(s3UrlResult);
        string expectedS3Key = s3Uri.Key;
        
        // Assert 2: Fetch object metadata directly from the real S3 in LocalStack to ensure the file exists
        GetObjectMetadataResponse? s3ObjectMetadata = await _s3Client.GetObjectMetadataAsync(
            IntegrationTestFixture.TargetBucketName, 
            expectedS3Key, 
            TestContext.Current.CancellationToken);

        Assert.NotNull(s3ObjectMetadata);
        Assert.Equal(contentType, s3ObjectMetadata.Headers.ContentType);
    }
}