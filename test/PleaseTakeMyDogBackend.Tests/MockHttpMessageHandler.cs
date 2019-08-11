using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PleaseTakeMyDogBackend.Tests
{
	class MockHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> action;

		public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> action)
		{
			this.action = action;
		}
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(this.action(request));
		}
	}
}
