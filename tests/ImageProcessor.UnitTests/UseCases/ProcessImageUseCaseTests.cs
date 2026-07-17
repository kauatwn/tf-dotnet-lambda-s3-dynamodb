using ImageProcessor.Lambda.Core;
using ImageProcessor.Lambda.DTOs;
using ImageProcessor.Lambda.Models;
using Moq;

namespace ImageProcessor.UnitTests.UseCases;

public class ProcessImageUseCaseTests
{
    private readonly Mock<IStorage> _storageMock = new();
    private readonly Mock<IImageRepository> _repositoryMock = new();

    private readonly ProcessImageUseCase _sut;

    public ProcessImageUseCaseTests()
    {
        _sut = new ProcessImageUseCase(_storageMock.Object, _repositoryMock.Object);
    }

    [Fact(DisplayName = "ExecuteAsync should upload to S3, save to DynamoDB and return metadata")]
    public async Task ExecuteAsync_ShouldProcessSuccessfully()
    {
        // Arrange
        const string validBase64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
        const string expectedFileName = "photo.png";
        const string expectedS3Url = $"s3://my-bucket/{expectedFileName}";
        
        ImageUploadRequest request = new(expectedFileName, "image/png", validBase64Image);
        
        _storageMock
            .Setup(s => s.UploadBase64ImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedS3Url);

        // Act
        ImageMetadata result = await _sut.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedFileName, result.FileName);
        Assert.Equal(expectedS3Url, result.S3Url);
        Assert.NotEqual(Guid.Empty.ToString(), result.ImageId);
        Assert.True(result.SizeInBytes > 0);
        
        _repositoryMock.Verify(r => r.SaveMetadataAsync(It.Is<ImageMetadata>(m => m.ImageId == result.ImageId)), Times.Once);
    }
}