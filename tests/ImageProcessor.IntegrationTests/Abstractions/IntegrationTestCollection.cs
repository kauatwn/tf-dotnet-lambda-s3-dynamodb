namespace ImageProcessor.IntegrationTests.Abstractions;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;