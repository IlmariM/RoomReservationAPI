using Microsoft.EntityFrameworkCore;
using RoomReservationAPI.Models;

namespace RoomReservationAPI.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<RoomReservation> RoomReservations { get; set; }
    }
}