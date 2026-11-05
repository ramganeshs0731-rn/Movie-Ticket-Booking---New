using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTicketBooking.Models
{
    [Table("Screens")]
    public class Screen
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScreenId { get; set; }

        [Required]
        public int TheatreId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ScreenName { get; set; } = string.Empty;

        // Navigation property back to Theatre (One-to-Many)
        [ForeignKey("TheatreId")]
        public Theatre Theatre { get; set; } = null!;

        // Navigation properties for Seats and Shows (will be enabled as those models are built)
        // public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        // public ICollection<Show> Shows { get; set; } = new List<Show>();
    }
}