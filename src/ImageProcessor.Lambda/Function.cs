using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using ImageProcessor.Lambda.Core;
using ImageProcessor.Lambda.DTOs;
using ImageProcessor.Lambda.Infrastructure;
using ImageProcessor.Lambda.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ImageProcessor.Lambda;

public partial class Function(ILogger<Function> logger, ProcessImageUseCase processImageUseCase)
{
    private static readonly Dictionary<string, string> JsonHeaders = new() { { "Content-Type", "application/json" } };

    public static async Task Main()
    {
        ServiceCollection services = new();
        services.AddInfrastructure();

        ServiceProvider sp = services.BuildServiceProvider();
        Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> handler = sp.GetRequiredService<Function>().FunctionHandler;

        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        LogReceivingRequest(logger);

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            LogBadRequest(logger, "Empty or null request body.");
            return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("Request body cannot be empty or invalid."), LambdaFunctionJsonSerializerContext.Default.ErrorResponse);
        }

        try
        {
            ImageUploadRequest? uploadRequest = JsonSerializer.Deserialize<ImageUploadRequest>(request.Body, LambdaFunctionJsonSerializerContext.Default.ImageUploadRequest);

            if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.Base64Image))
            {
                LogBadRequest(logger, "Invalid payload structure or missing Base64 image data.");
                return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("Invalid payload or missing Base64 image data."), LambdaFunctionJsonSerializerContext.Default.ErrorResponse);
            }

            ImageMetadata resultMetadata = await processImageUseCase.ExecuteAsync(uploadRequest);
            SuccessResponse<ImageMetadata> successResponse = new("Image processed successfully!", resultMetadata);

            return CreateResponse(HttpStatusCode.OK, successResponse, LambdaFunctionJsonSerializerContext.Default.SuccessResponseImageMetadata);
        }
        catch (Exception ex)
        {
            LogCriticalError(logger, ex.Message, ex);
            return CreateResponse(HttpStatusCode.InternalServerError, new ErrorResponse("Internal server error during image processing."), LambdaFunctionJsonSerializerContext.Default.ErrorResponse);
        }
    }

    private static APIGatewayProxyResponse CreateResponse<T>(HttpStatusCode statusCode, T body, JsonTypeInfo<T> jsonTypeInfo)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(body, jsonTypeInfo),
            Headers = JsonHeaders
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Receiving HTTP request from API Gateway.")]
    static partial void LogReceivingRequest(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bad Request: {Reason}")]
    static partial void LogBadRequest(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "A critical error occurred while processing the image. Error: {ErrorMessage}")]
    static partial void LogCriticalError(ILogger logger, string errorMessage, Exception ex);
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ImageUploadRequest))]
[JsonSerializable(typeof(ImageMetadata))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(SuccessResponse<ImageMetadata>))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext;