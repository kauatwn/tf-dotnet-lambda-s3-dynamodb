using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Testcontainers.LocalStack;

namespace ImageProcessor.IntegrationTests.Abstractions;

public class IntegrationTestFixture : IAsyncLifetime
{
    private const string TargetBucketName = "integration-test-bucket";
    private const string TargetTableName = "ImageMetadata";

    public AmazonS3Client? S3Client { get; private set; }
    public AmazonDynamoDBClient? DynamoClient { get; private set; }
    
    public LocalStackContainer LocalStack { get; } = new LocalStackBuilder("localstack/localstack:latest")
        .WithEnvironment("LOCALSTACK_AUTH_TOKEN", Environment.GetEnvironmentVariable("LOCALSTACK_AUTH_TOKEN"))
        .Build();

    public async ValueTask InitializeAsync()
    {
        await LocalStack.StartAsync();

        string serviceUrl = LocalStack.GetConnectionString();

        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", serviceUrl);
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");

        Environment.SetEnvironmentVariable("BUCKET_NAME", TargetBucketName);
        Environment.SetEnvironmentVariable("TABLE_NAME", TargetTableName);

        AmazonS3Config s3Config = new() { ServiceURL = serviceUrl, ForcePathStyle = true };
        AmazonDynamoDBConfig dynamoConfig = new() { ServiceURL = serviceUrl };

        S3Client = new AmazonS3Client(s3Config);
        DynamoClient = new AmazonDynamoDBClient(dynamoConfig);

        await S3Client.PutBucketAsync(TargetBucketName);

        await DynamoClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = TargetTableName,
            AttributeDefinitions = [new AttributeDefinition("ImageId", ScalarAttributeType.S)],
            KeySchema = [new KeySchemaElement("ImageId", KeyType.HASH)],
            BillingMode = BillingMode.PAY_PER_REQUEST
        });

        // AWS Best Practice: Await table status until ACTIVE to guarantee test stability
        await WaitUntilTableIsActiveAsync(DynamoClient, TargetTableName);
    }

    private static async Task WaitUntilTableIsActiveAsync(AmazonDynamoDBClient dynamoClient, string tableName)
    {
        string status = string.Empty;

        while (status != "ACTIVE")
        {
            await Task.Delay(500);
            try
            {
                DescribeTableResponse? response = await dynamoClient.DescribeTableAsync(tableName);
                status = response.Table.TableStatus;
            }
            catch (ResourceNotFoundException)
            {
                // Table metadata might not be immediately available via API
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        S3Client?.Dispose();
        DynamoClient?.Dispose();

        await LocalStack.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}