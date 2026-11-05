using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTicketBooking.DTOs.Bookings;
using MovieTicketBooking.Services.Interfaces;
using System.Threading.Tasks;

namespace MovieTicketBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // Protected by the custom Owner Authorization Policy
        [HttpPost]
        [Authorize(Policy = "SameUserBookingPolicy")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var createdBooking = await _bookingService.CreateBookingAsync(request);

            if (createdBooking == null)
            {
                return BadRequest(new
                {
                    message = "Booking failed. One or more seats are invalid or already booked."
                });
            }

            return Ok(createdBooking);
        }

        // Restricted STRICTLY to Admins
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBookingsForAdmin()
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            return Ok(bookings);
        }
    }
}
