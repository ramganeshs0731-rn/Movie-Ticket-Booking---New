using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MovieTicketBooking.Middlewares
{
    public class EndpointMetadataMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EndpointMetadataMiddleware> _logger;

        public EndpointMetadataMiddleware(
            RequestDelegate next,
            ILogger<EndpointMetadataMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation(
                "\n========== ENDPOINT METADATA ==========\n"
            );
            var endpoint = context.GetEndpoint();

            _logger.LogInformation(
                "Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (endpoint is null)
            {
                _logger.LogWarning("No endpoint selected.");
            }
            else
            {
                _logger.LogInformation(
                    "Endpoint: {Endpoint}",
                    endpoint.DisplayName);

                var allowAnonymous =
                    endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

                var authorizeData =
                    endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();

                _logger.LogInformation(
                    "AllowAnonymous: {AllowAnonymous}",
                    allowAnonymous);

                _logger.LogInformation(
                    "Authorization metadata count: {Count}",
                    authorizeData.Count);
            }

            await _next(context);
        }
    }
}
