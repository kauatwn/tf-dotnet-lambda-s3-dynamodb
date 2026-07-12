using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using ImageProcessor.Lambda.Core;
using ImageProcessor.Lambda.DTOs;
using ImageProcessor.Lambda.Infrastructure;
using ImageProcessor.Lambda.Models;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ImageProcessor.Lambda;

public partial class Function
{
    private static readonly S3Storage Storage = new(new AmazonS3Client());
    private static readonly DynamoDbRepository Repository = new(new AmazonDynamoDBClient());
    private static readonly ProcessImageUseCase ProcessImageUseCase = new(Storage, Repository);

    private static ILogger<Function>? _logger;

    private static async Task Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddLambdaLogger());
        _logger = loggerFactory.CreateLogger<Function>();

        Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> handler = FunctionHandler;

        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    private static async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        LogReceivingRequest(_logger);

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            LogBadRequest(_logger, "Empty or null request body.");
            ErrorResponse errorResponse = new("Request body cannot be empty or invalid.");

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = JsonSerializer.Serialize(errorResponse, LambdaFunctionJsonSerializerContext.Default.ErrorResponse),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        try
        {
            ImageUploadRequest? uploadRequest = JsonSerializer.Deserialize<ImageUploadRequest>(
                request.Body,
                LambdaFunctionJsonSerializerContext.Default.ImageUploadRequest);

            if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.Base64Image))
            {
                LogBadRequest(_logger, "Invalid payload structure or missing Base64 image data.");
                ErrorResponse errorResponse = new("Invalid payload or missing Base64 image data.");

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = JsonSerializer.Serialize(errorResponse, LambdaFunctionJsonSerializerContext.Default.ErrorResponse),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            ImageMetadata resultMetadata = await ProcessImageUseCase.ExecuteAsync(uploadRequest);
            SuccessResponse<ImageMetadata> successResponse = new("Image processed successfully!", resultMetadata);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(successResponse, LambdaFunctionJsonSerializerContext.Default.SuccessResponseImageMetadata),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            LogCriticalError(_logger, ex.Message, ex);
            ErrorResponse errorResponse = new("Internal server error during image processing.");

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = JsonSerializer.Serialize(errorResponse, LambdaFunctionJsonSerializerContext.Default.ErrorResponse),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Receiving HTTP request from API Gateway.")]
    static partial void LogReceivingRequest(ILogger? logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bad Request: {Reason}")]
    static partial void LogBadRequest(ILogger? logger, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "A critical error occurred while processing the image. Error: {ErrorMessage}")]
    static partial void LogCriticalError(ILogger? logger, string errorMessage, Exception ex);
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ImageUploadRequest))]
[JsonSerializable(typeof(ImageMetadata))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(SuccessResponse<ImageMetadata>))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext;