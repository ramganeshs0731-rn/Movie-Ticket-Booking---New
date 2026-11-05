using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MovieTicketBooking.Data;
using MovieTicketBooking.DTOs.Bookings;
using MovieTicketBooking.DTOs.ShowSeats;
using MovieTicketBooking.Models;
using MovieTicketBooking.Repositories.Interfaces;
using MovieTicketBooking.Services.Interfaces;

namespace MovieTicketBooking.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBookingRepository _bookingRepository;
        private readonly IShowRepository _showRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IShowSeatRepository _showSeatRepository;

        public BookingService(
            ApplicationDbContext context,
            IBookingRepository bookingRepository,
            IShowRepository showRepository,
            ISeatRepository seatRepository,
            IShowSeatRepository showSeatRepository)
        {
            _context = context;
            _bookingRepository = bookingRepository;
            _showRepository = showRepository;
            _seatRepository = seatRepository;
            _showSeatRepository = showSeatRepository;
        }

        public async Task<IEnumerable<ShowSeatResponse>> GetShowSeatsAsync(int showId)
        {
            var show = await _showRepository.GetByIdAsync(showId);
            if (show == null) return Enumerable.Empty<ShowSeatResponse>();

            var showSeats = await _showSeatRepository.GetByShowIdAsync(showId);

            // Auto-generate show seats if they haven't been initialized for this show yet
            if (!showSeats.Any())
            {
                var screenSeats = await _seatRepository.GetByScreenIdAsync(show.ScreenId);
                var newShowSeats = screenSeats.Select(seat => new ShowSeat
                {
                    ShowId = showId,
                    SeatId = seat.SeatId,
                    Status = "Available"
                }).ToList();

                await _showSeatRepository.AddRangeAsync(newShowSeats);
                showSeats = await _showSeatRepository.GetByShowIdAsync(showId);
            }

            return showSeats.Select(ss => new ShowSeatResponse
            {
                ShowSeatId = ss.ShowSeatId,
                ShowId = ss.ShowId,
                SeatId = ss.SeatId,
                SeatNumber = ss.Seat?.SeatNumber ?? string.Empty,
                SeatType = ss.Seat?.SeatType ?? string.Empty,
                Status = ss.Status
            });
        }

        public async Task<IEnumerable<BookingResponse>> GetAllBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();
            return bookings.Select(MapToResponse);
        }

        public async Task<BookingResponse?> GetBookingByIdAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            return booking == null ? null : MapToResponse(booking);
        }

        public async Task<BookingResponse?> CreateBookingAsync(CreateBookingRequest request)
        {
            var show = await _showRepository.GetByIdAsync(request.ShowId);
            if (show == null) return null;

            // Ensure show seats are initialized
            await GetShowSeatsAsync(request.ShowId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var showSeats = await _showSeatRepository.GetByIdsAsync(request.SeatIds);

                // Validate all seats exist and are available
                if (showSeats.Count() != request.SeatIds.Count || showSeats.Any(ss => ss.Status != "Available"))
                {
                    return null; // One or more seats invalid or already booked
                }

                // Mark seats as booked
                foreach (var ss in showSeats)
                {
                    ss.Status = "Booked";
                }
                await _showSeatRepository.UpdateRangeAsync(showSeats);

                var booking = new Booking
                {
                    ShowId = request.ShowId,
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    TotalPrice = show.Price * request.SeatIds.Count,
                    Status = "Confirmed",
                    BookingSeats = showSeats.Select(ss => new BookingSeat
                    {
                        ShowSeatId = ss.ShowSeatId
                    }).ToList()
                };

                var createdBooking = await _bookingRepository.AddAsync(booking);
                await transaction.CommitAsync();

                return await GetBookingByIdAsync(createdBooking.BookingId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static BookingResponse MapToResponse(Booking booking)
        {
            return new BookingResponse
            {
                BookingId = booking.BookingId,
                ShowId = booking.ShowId,
                MovieTitle = booking.Show?.Movie?.Title ?? string.Empty,
                TheatreName = booking.Show?.Screen?.Theatre?.Name ?? string.Empty,
                ScreenName = booking.Show?.Screen?.ScreenName ?? string.Empty,
                CustomerName = booking.CustomerName,
                CustomerEmail = booking.CustomerEmail,
                BookingTime = booking.BookingTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status,
                BookedSeatNumbers = booking.BookingSeats?.Select(bs => bs.ShowSeat?.Seat?.SeatNumber ?? string.Empty).ToList() ?? new List<string>()
            };
        }
    }
}
