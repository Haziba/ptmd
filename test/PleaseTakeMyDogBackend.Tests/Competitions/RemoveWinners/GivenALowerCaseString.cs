using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace PleaseTakeMyDogBackend.Tests.Competitions.RemoveWinners
{
	public class GivenALowerCaseString : IAsyncLifetime
	{
	    private string data;
	    private APIGatewayProxyResponse result;

	    public async Task InitializeAsync()
		{
		    this.data = "cool data";

			var handler = new ToUpperStringRequestResponseHandler(NullLogger<ToUpperStringRequestResponseHandler>.Instance);

		    this.result = await handler.HandleAsync(new APIGatewayProxyRequest
		    {
                Body = this.data
		    }, new TestLambdaContext());
		}

		public Task DisposeAsync() => Task.CompletedTask;

		[Fact]
		public void ThenAnAcceptedResultIsReturned()
		{
			this.result.Body
				.Should()
				.Be(this.data.ToUpper());
		}
	}
}
