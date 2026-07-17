using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ImageProcessor.IntegrationTests.Abstractions; 
using ImageProcessor.Lambda.Infrastructure;
using ImageProcessor.Lambda.Models;

namespace ImageProcessor.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public class DynamoDbRepositoryTests
{
    private readonly DynamoDbRepository _sut;
    private readonly AmazonDynamoDBClient _dynamoClient;
    
    private const string DbTableName = nameof(ImageMetadata);

    public DynamoDbRepositoryTests(IntegrationTestFixture fixture)
    {
        // Creates the client pointing to the LocalStack Docker container
        _dynamoClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            ServiceURL = fixture.LocalStack.GetConnectionString()
        });

        _sut = new DynamoDbRepository(_dynamoClient);
    }

    [Fact(DisplayName = "SaveMetadataAsync should persist the item correctly in the DynamoDB table")]
    public async Task SaveMetadataAsync_ShouldPersistItem()
    {
        // Arrange
        ImageMetadata metadata = new(
            ImageId: Guid.NewGuid().ToString(),
            FileName: "test-image.jpg",
            SizeInBytes: 1024L, 
            S3Url: "https://s3.amazonaws.com/bucket/test-image.jpg",
            UploadDate: DateTime.UtcNow
        );

        // Act
        await _sut.SaveMetadataAsync(metadata);

        // Assert
        GetItemRequest request = new()
        {
            TableName = DbTableName, // Name configured in your Fixture
            Key = new Dictionary<string, AttributeValue>
            {
                { nameof(ImageMetadata.ImageId), new AttributeValue { S = metadata.ImageId } }
            }
        };

        GetItemResponse? response = await _dynamoClient.GetItemAsync(request, TestContext.Current.CancellationToken);

        Assert.True(response.IsItemSet);
        Assert.Equal(metadata.FileName, response.Item[nameof(ImageMetadata.FileName)].S);
        Assert.Equal(metadata.S3Url, response.Item[nameof(ImageMetadata.S3Url)].S);
    }
}