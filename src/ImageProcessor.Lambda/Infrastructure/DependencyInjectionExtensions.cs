using System.Diagnostics.CodeAnalysis;
using Amazon.DynamoDBv2;
using Amazon.S3;
using ImageProcessor.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageProcessor.Lambda.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        AddAwsServices(services);
        AddBusinessServices(services);

        services.AddSingleton<Function>();

        return services;
    }

    private static void AddAwsServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddLambdaLogger());
        
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
        services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());
    }

    private static void AddBusinessServices(IServiceCollection services)
    {
        services.AddSingleton<IStorage, S3Storage>();
        services.AddSingleton<IImageRepository, DynamoDbRepository>();
        
        services.AddTransient<ProcessImageUseCase>();
    }
}