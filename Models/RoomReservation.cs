namespace RoomReservationAPI.Models
{
    public class RoomReservation
    {
        public int Id { get; set; }
        public int RoomNumber { get; set; } // Range 1-5, can add validation later
        public DateTime ReservationStart { get; set; }
        public DateTime ReservationEnd { get; set; }
        public string? ReserverName { get; set; }
        public string TimeZoneId { get; set; } = "UTC"; // IANA timezone identifier (e.g., "Europe/Helsinki", "America/New_York")
    }
}