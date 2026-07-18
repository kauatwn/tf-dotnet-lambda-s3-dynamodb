using Amazon.DynamoDBv2;
using Amazon.S3;
using ImageProcessor.Core.Interfaces;
using ImageProcessor.Infrastructure.Repositories;
using ImageProcessor.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessor.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // 1. Registra os clientes nativos da AWS
        services.AddSingleton<IAmazonS3>(new AmazonS3Client());
        services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient());

        // 2. Faz o binding (bind) das interfaces do Core com as implementações da Infra
        services.AddSingleton<IStorageService, S3StorageService>();
        services.AddSingleton<IMetadataRepository, DynamoDbRepository>();

        return services;
    }
}