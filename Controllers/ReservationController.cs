using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservationAPI.Data;
using RoomReservationAPI.Classes;
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

        /// <summary>
        /// Converts a DateTime from the specified timezone to UTC
        /// </summary>
        private DateTime ConvertToUtc(DateTime dateTime, string timeZoneId)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                // Assume the incoming datetime is in the specified timezone and convert to UTC
                return TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZone);
            }
            catch
            {
                // If timezone is invalid, assume it's already UTC
                return dateTime;
            }
        }

        /// <summary>
        /// Converts a DateTime from UTC to the specified timezone
        /// </summary>
        private DateTime ConvertFromUtc(DateTime utcDateTime, string timeZoneId)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            }
            catch
            {
                // If timezone is invalid, return as UTC
                return utcDateTime;
            }
        }

        // GET: api/Reservation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomReservation>>> GetReservations()
        {
            var reservations = await _context.RoomReservations.ToListAsync();

            // Convert times back from UTC to each reservation's timezone for response
            var responseReservations = reservations.Select(r => new RoomReservation
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                ReservationStart = ConvertFromUtc(r.ReservationStart, r.TimeZoneId),
                ReservationEnd = ConvertFromUtc(r.ReservationEnd, r.TimeZoneId),
                ReserverName = r.ReserverName,
                TimeZoneId = r.TimeZoneId
            }).ToList();

            return responseReservations;
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

            // Convert times back from UTC to the reservation's timezone for response
            var responseReservation = new RoomReservation
            {
                Id = reservation.Id,
                RoomNumber = reservation.RoomNumber,
                ReservationStart = ConvertFromUtc(reservation.ReservationStart, reservation.TimeZoneId),
                ReservationEnd = ConvertFromUtc(reservation.ReservationEnd, reservation.TimeZoneId),
                ReserverName = reservation.ReserverName,
                TimeZoneId = reservation.TimeZoneId
            };

            return responseReservation;
        }

        // POST: api/Reservation
        [HttpPost]
        public async Task<ActionResult<RoomReservation>> PostReservation(RoomReservation reservation)
        {
            // Convert incoming times from the specified timezone to UTC for storage
            reservation.ReservationStart = ConvertToUtc(reservation.ReservationStart, reservation.TimeZoneId);
            reservation.ReservationEnd = ConvertToUtc(reservation.ReservationEnd, reservation.TimeZoneId);

            var validation = await ValidateReservationAsync(reservation);
            if (!validation.isValid)
            {
                return BadRequest(validation.errorMsg);
            }

            _context.RoomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Convert back to the reserver's timezone for response
            var responseReservation = new RoomReservation
            {
                Id = reservation.Id,
                RoomNumber = reservation.RoomNumber,
                ReservationStart = ConvertFromUtc(reservation.ReservationStart, reservation.TimeZoneId),
                ReservationEnd = ConvertFromUtc(reservation.ReservationEnd, reservation.TimeZoneId),
                ReserverName = reservation.ReserverName,
                TimeZoneId = reservation.TimeZoneId
            };

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, responseReservation);
        }

        // PUT: api/Reservation/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, RoomReservation reservation)
        {
            // Convert incoming times from the specified timezone to UTC for storage
            var utcStart = ConvertToUtc(reservation.ReservationStart, reservation.TimeZoneId);
            var utcEnd = ConvertToUtc(reservation.ReservationEnd, reservation.TimeZoneId);

            // Create a temporary reservation object with UTC times for validation
            var reservationForValidation = new RoomReservation
            {
                ReservationStart = utcStart,
                ReservationEnd = utcEnd,
                RoomNumber = reservation.RoomNumber,
                TimeZoneId = reservation.TimeZoneId
            };

            var (isValid, errorMsg) = await ValidateReservationAsync(reservationForValidation, id);
            if (!isValid)
            {
                return BadRequest(errorMsg);
            }

            RoomReservation existingReservation = await _context.RoomReservations.FindAsync(id);

            if (existingReservation == null)
            {
                return BadRequest($"No reservation with id: {id} found");
            }

            existingReservation.ReservationStart = utcStart;
            existingReservation.ReservationEnd = utcEnd;
            existingReservation.RoomNumber = reservation.RoomNumber;
            existingReservation.ReserverName = reservation.ReserverName;
            existingReservation.TimeZoneId = reservation.TimeZoneId;

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
            return Ok(new ApiResponse<RoomReservation>
            {
                Success = true,
                Message = "Reservation updated",
                Data = existingReservation
            });
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

        private async Task<(bool isValid, string? errorMsg)> ValidateReservationAsync(RoomReservation reservation, int? id = null)
        {
            if (reservation.ReservationStart >= reservation.ReservationEnd)
            {
                return (false, "Start time must be before end time.");
            }

            // Use DateTime.UtcNow for comparison since times are stored in UTC
            if (reservation.ReservationStart < DateTime.UtcNow)
            {
                return (false, "Start time must not be in the past.");
            }

            // this monstrosity is because when creating a new reservation 
            // you do not need to check if same id reservation already exists
            // but when updating you do as not to fail validation because of the old reservation
            bool overlapping;
            if (id is not null)
            {
                overlapping = await _context.RoomReservations
                .AnyAsync(r => r.Id != id &&
                                r.RoomNumber == reservation.RoomNumber &&
                                r.ReservationStart < reservation.ReservationEnd &&
                                r.ReservationEnd > reservation.ReservationStart);
            }
            else
            {
                overlapping = await _context.RoomReservations
                .AnyAsync(r => r.RoomNumber == reservation.RoomNumber &&
                               r.ReservationStart < reservation.ReservationEnd &&
                               r.ReservationEnd > reservation.ReservationStart);
            }

            if (overlapping)
            {
                return (false, "Reservation overlaps with an existing reservation for the same room.");
            }
            return (true, null);
        }
    }
}