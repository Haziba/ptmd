using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Newtonsoft.Json;
using RestSharp;
using Xunit;

namespace PleaseTakeMyDogBackend.IntegrationTests.Handler
{
    [Collection(nameof(ApiCollection))]
	public sealed class WhenSendingAString : IAsyncLifetime
	{
		private readonly ApiFixture fixture;

		private string data;
		private IRestResponse response;

	    public WhenSendingAString(ApiFixture fixture) =>
	        this.fixture = fixture;

		public async Task InitializeAsync()
		{
			this.data = "cool stuff";

			var request = new RestRequest("/test", Method.POST);
		    request.AddParameter("text", "cool stuff", ParameterType.RequestBody);

			this.response = await this.fixture.RestClient.ExecuteTaskAsync(request);
		}

	    public Task DisposeAsync()
	    {
	        return Task.CompletedTask;
	    }

	    [Fact]
		public void ThenTheResponseShouldBeOkayResponse()
		{
			this.response.StatusCode.Should().Be(HttpStatusCode.OK);

			var responseContent = this.response.Content;

			responseContent.Should().Be(this.data.ToUpper());
		}
	}
}
