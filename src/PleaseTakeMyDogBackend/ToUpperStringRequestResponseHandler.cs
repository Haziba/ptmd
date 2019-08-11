using System;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PleaseTakeMyDogBackend
{
    public class ToUpperStringRequestResponseHandler : IRequestResponseHandler<APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        private readonly ILogger<ToUpperStringRequestResponseHandler> _logger;

        public ToUpperStringRequestResponseHandler(ILogger<ToUpperStringRequestResponseHandler> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _logger = logger;
        }

        public Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest input, ILambdaContext context)
        {
            return Task.FromResult(new APIGatewayProxyResponse { Body = input.Body.ToUpper() });
        }
    }
}