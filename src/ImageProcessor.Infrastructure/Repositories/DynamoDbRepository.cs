using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ImageProcessor.Core.Interfaces;
using ImageProcessor.Core.Models;

namespace ImageProcessor.Infrastructure.Repositories;

public sealed class DynamoDbRepository(IAmazonDynamoDB dynamoDbClient) : IMetadataRepository
{
    private readonly string _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") 
        ?? throw new InvalidOperationException("The 'TABLE_NAME' environment variable is not configured.");
    
    public async Task SaveMetadataAsync(ImageMetadata metadata)
    {
        PutItemRequest request = new()
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "imageId", new AttributeValue { S = metadata.ImageId } },
                { "fileName", new AttributeValue { S = metadata.FileName } },
                { "sizeInBytes", new AttributeValue { N = metadata.SizeInBytes.ToString() } },
                { "s3Url", new AttributeValue { S = metadata.S3Url } },
                { "uploadDate", new AttributeValue { S = $"{metadata.UploadDate:O}" } } 
            }
        };

        await dynamoDbClient.PutItemAsync(request);
    }
}