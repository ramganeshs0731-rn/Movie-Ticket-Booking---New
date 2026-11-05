using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MovieTicketBooking.DTOs.Bookings;
using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieTicketBooking.Authorization
{
    public class SameUserBookingHandler : AuthorizationHandler<SameUserBookingRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SameUserBookingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SameUserBookingRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return;
            }

            // 1. Extract email securely from JWT token claims
            var tokenEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                             ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                             ?? httpContext.User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(tokenEmail))
            {
                return;
            }

            // 2. Enable buffering to read the request body stream before model binding
            httpContext.Request.EnableBuffering();
            using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
            var bodyString = await reader.ReadToEndAsync();

            // Reset stream position so the model binder can read it later
            httpContext.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(bodyString))
            {
                return;
            }

            try
            {
                // 3. Deserialize request body to inspect CustomerEmail
                var bookingRequest = JsonSerializer.Deserialize<CreateBookingRequest>(bodyString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (bookingRequest != null)
                {
                    // 4. Enforce Owner Authorization: Token email must match request body email
                    if (tokenEmail.Equals(bookingRequest.CustomerEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Succeed(requirement); // Authorization passed!
                    }
                }
            }
            catch
            {
                // JSON parsing errors result in failed authorization automatically
            }
        }
    }
}
