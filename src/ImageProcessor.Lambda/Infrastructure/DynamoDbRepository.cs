using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ImageProcessor.Lambda.Core;
using ImageProcessor.Lambda.Models;

namespace ImageProcessor.Lambda.Infrastructure;

public sealed class DynamoDbRepository(IAmazonDynamoDB dynamoDbClient) : IImageRepository
{
    private readonly string _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ??
                                         throw new ArgumentNullException("TABLE_NAME não configurada.");

    public async Task SaveMetadataAsync(ImageMetadata metadata)
    {
        PutItemRequest request = new()
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "ImageId", new AttributeValue { S = metadata.ImageId } },
                { "FileName", new AttributeValue { S = metadata.FileName } },
                { "SizeInBytes", new AttributeValue { N = metadata.SizeInBytes.ToString() } },
                { "S3Url", new AttributeValue { S = metadata.S3Url } },
                { "UploadDate", new AttributeValue { S = metadata.UploadDate.ToString("O") } } // Formato ISO 8601
            }
        };

        await dynamoDbClient.PutItemAsync(request);
    }
}