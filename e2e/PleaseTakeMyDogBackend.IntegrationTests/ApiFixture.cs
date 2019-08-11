using RestSharp;
using Xunit.Abstractions;

namespace PleaseTakeMyDogBackend.IntegrationTests
{
    public class ApiFixture : Fixture
    {
		public RestClient RestClient { get; }

		public ApiFixture(IMessageSink messageSink) : base(messageSink)
		{
			this.RestClient = new RestClient($"http://127.0.0.1:3000");
		}
    }
}