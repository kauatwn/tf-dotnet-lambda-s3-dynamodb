using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessor.IntegrationTests.Abstractions;
using ImageProcessor.Lambda;
using ImageProcessor.Lambda.Core;
using ImageProcessor.Lambda.DTOs;
using ImageProcessor.Lambda.Infrastructure;
using ImageProcessor.Lambda.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImageProcessor.IntegrationTests.Handlers;

[Collection(nameof(IntegrationTestCollection))]
[Trait("Category", "Integration")]
public class FunctionTests
{
    private readonly AmazonS3Client _s3Client;
    private readonly AmazonDynamoDBClient _dynamoClient;

    private const string DbTableName = nameof(ImageMetadata);

    private readonly Function _sut;
    
    public FunctionTests(IntegrationTestFixture fixture)
    {
        _s3Client = fixture.S3Client ?? throw new InvalidOperationException("The AmazonS3Client dependency in the test fixture was not initialized. Ensure LocalStack started successfully.");
        _dynamoClient = fixture.DynamoClient ?? throw new InvalidOperationException("The AmazonDynamoDBClient dependency in the test fixture was not initialized. Check DynamoDB table provisioning status.");

        S3Storage storage = new(_s3Client);
        DynamoDbRepository repository = new(_dynamoClient);

        ProcessImageUseCase useCase = new(storage, repository);
        _sut = new Function(NullLogger<Function>.Instance, useCase);
    }

    [Fact(DisplayName = "FunctionHandler should return 200 OK, upload to S3 and save metadata in DynamoDB")]
    public async Task FunctionHandler_ShouldProcessImage_AndCompleteSuccessfully()
    {
        // Arrange
        const string fileName = "integration-test.png";
        const string base64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
        
        ImageUploadRequest requestPayload = new(fileName, "image/png", base64Image);
        
        APIGatewayProxyRequest request = new()
        {
            Body = JsonSerializer.Serialize(requestPayload, LambdaFunctionJsonSerializerContext.Default.ImageUploadRequest)
        };
        
        TestLambdaContext context = new();

        // Act
        APIGatewayProxyResponse response = await _sut.FunctionHandler(request, context);

        // Assert 1: API Gateway Response Validation
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Image processed successfully!", response.Body);

        SuccessResponse<ImageMetadata>? responseBody = JsonSerializer.Deserialize<SuccessResponse<ImageMetadata>>(response.Body, LambdaFunctionJsonSerializerContext.Default.SuccessResponseImageMetadata);
            
        Assert.NotNull(responseBody);
        Assert.NotEmpty(responseBody.Message);
        
        string generatedImageId = responseBody.Data.ImageId;
        Assert.NotEqual(string.Empty, generatedImageId);

        // Assert 2: DynamoDB Validation (Reusing the class-level client)
        GetItemResponse dynamoItem = await _dynamoClient.GetItemAsync(DbTableName, new Dictionary<string, AttributeValue>
        {
            { nameof(ImageMetadata.ImageId), new AttributeValue { S = generatedImageId } }
        }, TestContext.Current.CancellationToken);
        
        Assert.True(dynamoItem.IsItemSet, "Metadata record was not found inside the DynamoDB engine table.");
        Assert.Equal(fileName, dynamoItem.Item[nameof(ImageMetadata.FileName)].S);

        // Assert 3: S3 Validation (Reusing the class-level client)
        string expectedS3Key = dynamoItem.Item[nameof(ImageMetadata.S3Url)].S.Replace("s3://integration-test-bucket/", "");
        GetObjectMetadataResponse? s3Object = await _s3Client.GetObjectMetadataAsync("integration-test-bucket", expectedS3Key, TestContext.Current.CancellationToken);
        
        Assert.NotNull(s3Object);
        Assert.Equal("image/png", s3Object.Headers.ContentType);
    }

    [Fact(DisplayName = "FunctionHandler should return 400 Bad Request when Base64Image is missing or empty")]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenBase64IsMissing()
    {
        // Arrange
        ImageUploadRequest requestPayload = new("test.png", "image/png", string.Empty);
        APIGatewayProxyRequest request = new()
        {
            Body = JsonSerializer.Serialize(requestPayload, LambdaFunctionJsonSerializerContext.Default.ImageUploadRequest)
        };
        TestLambdaContext context = new();

        // Act
        APIGatewayProxyResponse response = await _sut.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? errorBody = JsonSerializer.Deserialize<ErrorResponse>(response.Body, LambdaFunctionJsonSerializerContext.Default.ErrorResponse);
        
        Assert.NotNull(errorBody);
        Assert.NotEmpty(errorBody.Error);
    }

    [Fact(DisplayName = "FunctionHandler should return 400 Bad Request when body is empty")]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenBodyIsEmpty()
    {
        // Arrange
        APIGatewayProxyRequest request = new() { Body = null };
        TestLambdaContext context = new();

        // Act
        APIGatewayProxyResponse response = await _sut.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? errorBody = JsonSerializer.Deserialize<ErrorResponse>(response.Body, LambdaFunctionJsonSerializerContext.Default.ErrorResponse);
        
        Assert.NotNull(errorBody);
        Assert.NotEmpty(errorBody.Error);
    }
}