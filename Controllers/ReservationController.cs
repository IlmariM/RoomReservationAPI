using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservationAPI.Data;
using RoomReservationAPI.Models;

namespace RoomReservationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomReservation>>> GetReservations()
        {
            return await _context.RoomReservations.ToListAsync();
        }

        // GET: api/Reservation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomReservation>> GetReservation(int id)
        {
            var reservation = await _context.RoomReservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return reservation;
        }

        // POST: api/Reservation
        [HttpPost]
        public async Task<ActionResult<RoomReservation>> PostReservation(RoomReservation reservation)
        {
            if (reservation.ReservationStart >= reservation.ReservationEnd)
            {
                return BadRequest("Start time must be before end time.");
            }

            if (reservation.ReservationStart < DateTime.UtcNow)
            {
                return BadRequest("Start time must not be in the past.");
            }

            var overlapping = await _context.RoomReservations
                .AnyAsync(r => r.RoomNumber == reservation.RoomNumber &&
                               r.ReservationStart < reservation.ReservationEnd &&
                               r.ReservationEnd > reservation.ReservationStart);

            if (overlapping)
            {
                return BadRequest("Reservation overlaps with an existing reservation for the same room.");
            }

            _context.RoomReservations.Add(reservation);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
        }

        // PUT: api/Reservation/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, RoomReservation reservation)
        {
            if (reservation.ReservationStart >= reservation.ReservationEnd)
            {
                return BadRequest("Start time must be before end time.");
            }

            if (reservation.ReservationStart < DateTime.UtcNow)
            {
                return BadRequest("Start time must not be in the past.");
            }

            var overlapping = await _context.RoomReservations
                .AnyAsync(r => r.Id != id &&
                               r.RoomNumber == reservation.RoomNumber &&
                               r.ReservationStart < reservation.ReservationEnd &&
                               r.ReservationEnd > reservation.ReservationStart);

            if (overlapping)
            {
                return BadRequest("Reservation overlaps with an existing reservation for the same room.");
            }

            
            RoomReservation existingReservation = await _context.RoomReservations.FindAsync(id);

            if (existingReservation == null)
            {
                return BadRequest($"No reservation with id: {id} found");
            }
            
            existingReservation.ReservationStart = reservation.ReservationStart;
            existingReservation.ReservationEnd = reservation.ReservationEnd;
            existingReservation.RoomNumber = reservation.RoomNumber;
            existingReservation.ReserverName = reservation.ReserverName;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok($"Updated reservation for room: {reservation.RoomNumber}");
        }

        // DELETE: api/Reservation/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.RoomReservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            _context.RoomReservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return Ok($"Removed reservation for room: {reservation.RoomNumber}");
        }

        private bool ReservationExists(int id)
        {
            return _context.RoomReservations.Any(e => e.Id == id);
        }
    }
}