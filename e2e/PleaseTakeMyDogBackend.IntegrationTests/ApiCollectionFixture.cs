using Xunit;

namespace PleaseTakeMyDogBackend.IntegrationTests
{
    [CollectionDefinition(nameof(ApiCollection))]
    public class ApiCollection : ICollectionFixture<ApiFixture>
    {
    }
}