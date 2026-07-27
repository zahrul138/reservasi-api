using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasiAPI.Repository;
using ReservasiAPI.Repository.Models;
using System.ComponentModel.DataAnnotations;

namespace ReservasiAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly ReservasiDbContext _context;

        public ReviewController(ReservasiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return await _context.Reviews
                .Include(r => r.Booking)
                .Include(r => r.Room)
                .Include(r => r.User)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.Booking)
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            return review ?? (ActionResult<Review>)NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<Review>> CreateReview([FromBody] Review reviewRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validasi booking
            var booking = await _context.Bookings.FindAsync(reviewRequest.BookingId);
            if (booking == null)
                return BadRequest("Booking not found");

            // Validasi kamar
            var room = await _context.Rooms.FindAsync(reviewRequest.RoomId);
            if (room == null)
                return BadRequest("Room not found");

            // Validasi user
            var user = await _context.Users.FindAsync(reviewRequest.UserId);
            if (user == null)
                return BadRequest("User not found");

            // Buat objek review baru
            var review = new Review
            {
                BookingId = reviewRequest.BookingId,
                RoomId = reviewRequest.RoomId,
                UserId = reviewRequest.UserId,
                Rating = reviewRequest.Rating,
                Title = reviewRequest.Title,
                Comment = reviewRequest.Comment,
                Fullname = user.Fullname ?? user.Email ?? $"user-{user.Id}", // Gunakan properti yang ada
                RoomType = room.Title,
                Status = "pending",
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateReviewStatus(int id, [FromBody] StatusUpdateDto dto)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            // No admin validation - simple status update
            review.Status = dto.Status;
            review.ModeratedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("room/{roomId}/approved")]
        public async Task<ActionResult<IEnumerable<Review>>> GetApprovedReviews(int roomId)
        {
            return await _context.Reviews
                .Where(r => r.RoomId == roomId && r.Status == "approved")
                .Include(r => r.User)
                .Include(r => r.Room)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        [HttpGet("approved")]
        public async Task<ActionResult<IEnumerable<Review>>> GetAllApprovedReviews([FromQuery] int limit = 5)
        {
            return await _context.Reviews
                .Where(r => r.Status == "approved")
                .Include(r => r.User)
                .Include(r => r.Room)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public class StatusUpdateDto
        {
            [Required]
            public string Status { get; set; } // "approved" or "rejected"
        }

        [HttpGet("user/{userId}/booking/{bookingId}")]
        public async Task<ActionResult<bool>> HasUserReviewedBooking(int userId, int bookingId)
        {
            var hasReview = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.BookingId == bookingId);
            return hasReview;
        }
    }
}