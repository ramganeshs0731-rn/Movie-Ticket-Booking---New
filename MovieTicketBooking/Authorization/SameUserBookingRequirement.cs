using Microsoft.AspNetCore.Authorization;

namespace MovieTicketBooking.Authorization
{
    public class SameUserBookingRequirement : IAuthorizationRequirement
    {
        // Marker class for the policy requirement
    }
}
