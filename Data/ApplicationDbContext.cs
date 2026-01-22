using Microsoft.EntityFrameworkCore;
using RoomReservationAPI.Models;

namespace RoomReservationAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<RoomReservation> RoomReservations { get; set; }
    }
}