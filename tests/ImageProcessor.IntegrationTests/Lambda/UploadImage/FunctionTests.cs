using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessor.Core.UseCases;
using ImageProcessor.Infrastructure.Repositories;
using ImageProcessor.Infrastructure.Storage;
using ImageProcessor.IntegrationTests.Abstractions;
using ImageProcessor.Lambda.UploadImage;
using ImageProcessor.Lambda.UploadImage.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImageProcessor.IntegrationTests.Lambda.UploadImage;

[Collection(nameof(IntegrationTestCollection))]
[Trait("Category", "Integration")]
public class FunctionTests
{
    private readonly AmazonS3Client _s3Client;
    private readonly AmazonDynamoDBClient _dynamoClient;

    private readonly Function _sut;
    
    public FunctionTests(IntegrationTestFixture fixture)
    {
        _s3Client = fixture.S3Client ?? throw new InvalidOperationException("The AmazonS3Client dependency in the test fixture was not initialized. Ensure LocalStack started successfully.");
        _dynamoClient = fixture.DynamoClient ?? throw new InvalidOperationException("The AmazonDynamoDBClient dependency in the test fixture was not initialized. Check DynamoDB table provisioning status.");

        S3StorageService storage = new(_s3Client);
        DynamoDbRepository repository = new(_dynamoClient);

        UploadImageUseCase useCase = new(storage, repository, NullLogger<UploadImageUseCase>.Instance);
        
        _sut = new Function(NullLogger<Function>.Instance, useCase);
    }

    [Fact(DisplayName = "FunctionHandler should return 200 OK, upload to S3 and save metadata in DynamoDB")]
    public async Task FunctionHandler_ShouldProcessImage_AndCompleteSuccessfully()
    {
        // Arrange
        const string fileName = "integration-test.png";
        const string base64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
        
        UploadImageRequest requestPayload = new(fileName, "image/png", base64Image);
        
        APIGatewayProxyRequest request = new()
        {
            Body = JsonSerializer.Serialize(requestPayload, LambdaFunctionJsonSerializerContext.Default.UploadImageRequest)
        };
        
        TestLambdaContext context = new();

        // Act
        APIGatewayProxyResponse response = await _sut.FunctionHandler(request, context);

        // Assert 1: API Gateway Response Validation
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        UploadImageResponse? responseBody = JsonSerializer.Deserialize<UploadImageResponse>(response.Body, LambdaFunctionJsonSerializerContext.Default.UploadImageResponse);
            
        Assert.NotNull(responseBody);
        Assert.NotEqual(string.Empty, responseBody.ImageId);
        
        string generatedImageId = responseBody.ImageId;

        // Assert 2: DynamoDB Validation
        GetItemResponse dynamoItem = await _dynamoClient.GetItemAsync(IntegrationTestFixture.TargetTableName, new Dictionary<string, AttributeValue>
        {
            { "imageId", new AttributeValue { S = generatedImageId } }
        }, TestContext.Current.CancellationToken);
        
        Assert.True(dynamoItem.IsItemSet, "Metadata record was not found inside the DynamoDB engine table.");
        Assert.Equal(fileName, dynamoItem.Item["fileName"].S);

        // Assert 3: S3 Validation
        string expectedS3Key = dynamoItem.Item["s3Url"].S.Replace("s3://integration-test-bucket/", "");
        GetObjectMetadataResponse? s3Object = await _s3Client.GetObjectMetadataAsync("integration-test-bucket", expectedS3Key, TestContext.Current.CancellationToken);
        
        Assert.NotNull(s3Object);
        Assert.Equal("image/png", s3Object.Headers.ContentType);
    }

    [Fact(DisplayName = "FunctionHandler should return 400 Bad Request when Base64Image is missing or empty")]
    public async Task FunctionHandler_ShouldReturnBadRequest_WhenBase64IsMissing()
    {
        // Arrange
        UploadImageRequest requestPayload = new("test.png", "image/png", string.Empty);
        APIGatewayProxyRequest request = new()
        {
            Body = JsonSerializer.Serialize(requestPayload, LambdaFunctionJsonSerializerContext.Default.UploadImageRequest)
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