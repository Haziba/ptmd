using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PleaseTakeMyDogBackend.IntegrationTests
{
	public class SamLocalLambdaInvoker : IDisposable
	{
		private readonly IMessageSink messageSink;
		private readonly AmazonLambdaClient client;

		public SamLocalLambdaInvoker(IMessageSink messageSink, int hostPort)
		{
			this.messageSink = messageSink;
			this.client = new AmazonLambdaClient(
				new BasicAWSCredentials("localstack", "localstack"),
				new AmazonLambdaConfig
				{
					ServiceURL = $"http://127.0.0.1:{hostPort}/",
					AuthenticationRegion = "eu-west-1"
				});
		}

		public async Task<string> InvokeAsync(string functionName, string payload)
		{
			var response = await this.client.InvokeAsync(new InvokeRequest
			{
				FunctionName = functionName,
				InvocationType = InvocationType.RequestResponse,
				Payload = payload
			});

			using (var sr = new StreamReader(response.Payload))
			{
				var responseContent = await sr.ReadToEndAsync();
				this.messageSink.OnMessage(new DiagnosticMessage($"Function: {functionName} Status: {response.StatusCode} Response: {responseContent}"));

				return responseContent;
			}
		}

		public void Dispose() => this.client.Dispose();
	}
}
