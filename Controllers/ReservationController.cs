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

        private readonly ILogger<ReservationController> _logger;

        public ReservationController(ApplicationDbContext context, ILogger<ReservationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Reservation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomReservation>>> GetReservations()
        {
            var reservations = await _context.RoomReservations.ToListAsync();

            // Convert times back from UTC to each reservation's timezone for response
            var responseReservations = reservations.Select(r => ToResponseReservation(r)).ToList();

            return responseReservations;
        }

        // GET: api/Reservation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomReservation>> GetReservation(int id)
        {
            var reservation = await _context.RoomReservations.FindAsync(id);
            if (reservation == null)
            {
                _logger.LogError("No reservatioin found with {id}", id);
                return NotFound();
            }

            // Convert times back from UTC to the reservation's timezone for response
            var responseReservation = ToResponseReservation(reservation);

            return responseReservation;
        }

        // GET: api/Reservation/NextReservations
        [HttpGet("NextReservations")]
        public async Task<ActionResult<IEnumerable<RoomReservation>>> GetNextReservations(DateTime? time = null, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Invalid pageNumber: {pageNumber}. Using default value 1.", pageNumber);
                pageNumber = 1;
            }
            if (pageSize < 1)
            {
                _logger.LogWarning("Invalid pageSize: {pageSize}. Using default value 10.", pageSize);
                pageSize = 10;
            }

            // Query reservations starting from the specified time
            var reservations = await _context.RoomReservations
                .Where(r => r.ReservationStart >= (time ?? DateTime.UtcNow))
                .OrderBy(r => r.ReservationStart)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Convert times back from UTC to each reservation's timezone for response
            var responseReservations = reservations.Select(r => ToResponseReservation(r)).ToList();

            return responseReservations;
        }

        // POST: api/Reservation
        [HttpPost]
        public async Task<ActionResult<RoomReservation>> PostReservation(RoomReservation reservation)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(reservation.TimeZoneId);
            }
            catch
            {
                _logger.LogError("Error validating timezoneId: {reservation.TimeZoneId}.", reservation.TimeZoneId);
                return BadRequest($"Error validating timezoneId: {reservation.TimeZoneId}. Input a valid Id.");
            }
            // Convert incoming times from the specified timezone to UTC for storage
            reservation.ReservationStart = ConvertToUtc(reservation.ReservationStart, reservation.TimeZoneId);
            reservation.ReservationEnd = ConvertToUtc(reservation.ReservationEnd, reservation.TimeZoneId);

            var (isValid, errorMsg) = await ValidateReservationAsync(reservation);
            if (!isValid)
            {
                _logger.LogError("Error validating reservation with message: {errorMsg}", errorMsg);
                return BadRequest(errorMsg);
            }

            _context.RoomReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Convert back to the reserver's timezone for response
            var responseReservation = ToResponseReservation(reservation);

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, responseReservation);
        }

        // PUT: api/Reservation/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, RoomReservation reservation)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(reservation.TimeZoneId);
            }
            catch
            {
                _logger.LogError("Error validating timezoneId: {reservation.TimeZoneId}.", reservation.TimeZoneId);
                return BadRequest($"Error validating timezoneId: {reservation.TimeZoneId}. Input a valid Id.");
            }
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
                _logger.LogError("Error validating reservation with message: {errorMsg}", errorMsg);
                return BadRequest(errorMsg);
            }

            RoomReservation? existingReservation = await _context.RoomReservations.FindAsync(id);

            if (existingReservation == null)
            {
                _logger.LogError("No reservation with id: {id} found", id);
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
                    _logger.LogError("Reservation with id: {id} was deleted before it could be updated", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Reservation with id: {id} was modified first by someone else", id);
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
                _logger.LogError("No reservation with id: {id} found", id);
                return NotFound();
            }
            _context.RoomReservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool ReservationExists(int id)
        {
            return _context.RoomReservations.Any(e => e.Id == id);
        }

        private RoomReservation ToResponseReservation(RoomReservation dbReservation)
        {
            return new RoomReservation
            {
                Id = dbReservation.Id,
                RoomNumber = dbReservation.RoomNumber,
                ReservationStart = ConvertFromUtc(dbReservation.ReservationStart, dbReservation.TimeZoneId),
                ReservationEnd = ConvertFromUtc(dbReservation.ReservationEnd, dbReservation.TimeZoneId),
                ReserverName = dbReservation.ReserverName,
                TimeZoneId = dbReservation.TimeZoneId
            };
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

            var overlapping = await _context.RoomReservations.AnyAsync(r =>
                r.RoomNumber == reservation.RoomNumber &&
                (id == null || r.Id != id) && // Exclude current reservation if updating
                r.ReservationStart < reservation.ReservationEnd &&
                r.ReservationEnd > reservation.ReservationStart);

            if (overlapping)
            {
                return (false, "Reservation overlaps with an existing reservation for the same room.");
            }
            return (true, null);
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
            catch (InvalidTimeZoneException) // This should no longer happen but i'm leaving it just in case
            {
                _logger.LogWarning("Invalid timezone: {TimeZoneId}", timeZoneId);
                return dateTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error converting timezone");
                throw;
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
            catch (InvalidTimeZoneException)
            {
                _logger.LogWarning("Invalid timezone: {TimeZoneId}", timeZoneId);
                return utcDateTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error converting timezone");
                throw;
            }
        }
    }
}