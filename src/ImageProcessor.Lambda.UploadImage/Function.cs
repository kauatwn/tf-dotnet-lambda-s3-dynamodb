using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using ImageProcessor.Core.Models;
using ImageProcessor.Core.UseCases;
using ImageProcessor.Infrastructure.Extensions;
using ImageProcessor.Lambda.UploadImage.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ImageProcessor.Lambda.UploadImage;

public partial class Function(ILogger<Function> logger, UploadImageUseCase uploadImageUseCase)
{
    private static readonly Dictionary<string, string> JsonHeaders = new() { { "Content-Type", "application/json" } };

    public static async Task Main()
    {
        ServiceCollection services = new();
        
        services.AddLogging(builder => builder.AddLambdaLogger());
        services.AddInfrastructure();
        services.AddTransient<UploadImageUseCase>();

        services.AddSingleton<Function>();

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
            UploadImageRequest? uploadRequest = JsonSerializer.Deserialize<UploadImageRequest>(
                request.Body, 
                LambdaFunctionJsonSerializerContext.Default.UploadImageRequest);

            if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.Base64Image))
            {
                LogBadRequest(logger, "Invalid payload structure or missing Base64 image data.");
                return CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse("Invalid payload or missing Base64 image data."), LambdaFunctionJsonSerializerContext.Default.ErrorResponse);
            }

            ImageMetadata metadata = await uploadImageUseCase.ExecuteAsync(uploadRequest.Base64Image, uploadRequest.FileName, uploadRequest.ContentType);
            UploadImageResponse responseBody = new(metadata.ImageId, metadata.S3Url, metadata.SizeInBytes, metadata.UploadDate);

            return CreateResponse(HttpStatusCode.OK, responseBody, LambdaFunctionJsonSerializerContext.Default.UploadImageResponse);
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
[JsonSerializable(typeof(UploadImageRequest))]
[JsonSerializable(typeof(UploadImageResponse))]
[JsonSerializable(typeof(ErrorResponse))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext;