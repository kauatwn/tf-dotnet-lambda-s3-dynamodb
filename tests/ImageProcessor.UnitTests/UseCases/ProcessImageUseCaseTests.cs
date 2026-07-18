using ImageProcessor.Core.Interfaces;
using ImageProcessor.Core.Models;
using ImageProcessor.Core.UseCases;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ImageProcessor.UnitTests.UseCases;

public class UploadImageUseCaseTests
{
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<IMetadataRepository> _repositoryMock = new();

    private readonly UploadImageUseCase _sut;

    public UploadImageUseCaseTests()
    {
        _sut = new UploadImageUseCase(_storageMock.Object, _repositoryMock.Object, NullLogger<UploadImageUseCase>.Instance);
    }

    [Fact(DisplayName = "ExecuteAsync should upload to S3, save to DynamoDB and return metadata")]
    public async Task ExecuteAsync_ShouldProcessSuccessfully()
    {
        // Arrange
        const string validBase64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
        const string expectedFileName = "photo.png";
        const string expectedContentType = "image/png";
        const string expectedS3Url = $"s3://my-bucket/{expectedFileName}";

        _storageMock
            .Setup(s => s.UploadBase64ImageAsync(validBase64Image, expectedFileName, expectedContentType))
            .ReturnsAsync(expectedS3Url);

        // Act
        ImageMetadata result = await _sut.ExecuteAsync(validBase64Image, expectedFileName, expectedContentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedFileName, result.FileName);
        Assert.Equal(expectedS3Url, result.S3Url);
        Assert.NotEqual(Guid.Empty.ToString(), result.ImageId);
        Assert.True(result.SizeInBytes > 0);

        _repositoryMock.Verify(r => r.SaveMetadataAsync(It.Is<ImageMetadata>(m => m.ImageId == result.ImageId)), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync should log error and rethrow exception if storage fails")]
    public async Task ExecuteAsync_ShouldThrowException_WhenStorageFails()
    {
        // Arrange
        const string validBase64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";

        _storageMock
            .Setup(s => s.UploadBase64ImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("AWS S3 Upload Failed"));

        // Act
        Task Act() => _sut.ExecuteAsync(validBase64Image, "error.png", "image/png");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(Act);

        _repositoryMock.Verify(r => r.SaveMetadataAsync(It.IsAny<ImageMetadata>()), Times.Never);
    }
}