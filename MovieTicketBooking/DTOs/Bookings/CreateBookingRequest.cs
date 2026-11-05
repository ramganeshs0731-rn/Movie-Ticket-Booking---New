using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MovieTicketBooking.DTOs.Bookings
{
    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "Show ID is required.")]
        public int ShowId { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer email is required.")]
        [EmailAddress]
        [MaxLength(150)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one seat must be selected.")]
        public List<int> SeatIds { get; set; } = new List<int>();
    }
}
