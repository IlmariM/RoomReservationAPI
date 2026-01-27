using System.ComponentModel.DataAnnotations;

namespace RoomReservationAPI.Models
{
    public class RoomReservation
    {
        public int Id { get; set; }
        [Range(1, 5, ErrorMessage = "RoomNumber must be between 1 and 5")]
        public int RoomNumber { get; set; }

        [Required(ErrorMessage = "Reservation start time is required")]
        public DateTime ReservationStart { get; set; }
        [Required(ErrorMessage = "Reservation end time is required")]
        public DateTime ReservationEnd { get; set; }

        [Required(ErrorMessage = "ReserverName is required")]
        [StringLength(100)]
        public string? ReserverName { get; set; }

        [Required]
        [StringLength(50)]
        public string TimeZoneId { get; set; } = "UTC"; // IANA timezone identifier (e.g., "Europe/Helsinki", "America/New_York")
    }
}