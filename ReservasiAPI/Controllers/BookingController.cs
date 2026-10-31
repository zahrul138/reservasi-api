using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasiAPI.Repository.Models;
using ReservasiAPI.Repository;
using System.Text.Json;

namespace ReservasiAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly ReservasiDbContext _context;

        public BookingController(ReservasiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            return await _context.Bookings.Include(b => b.User).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            var booking = await _context.Bookings.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return NotFound();

            return booking;
        }

        [HttpPost]
        public async Task<ActionResult<Booking>> CreateBooking(Booking booking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validasi ketersediaan kamar
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Title == booking.RoomType);
            if (room == null)
            {
                return BadRequest("Room type not found.");
            }
            booking.RoomId = room.Id;

            if (room.Quantity <= 0)
            {
                return BadRequest("This room type is fully booked.");
            }

            // Validasi user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == booking.UserId);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Validasi duplikat booking
            var isDuplicate = await _context.Bookings.AnyAsync(b =>
                b.Fullname == booking.Fullname &&
                b.Email == booking.Email &&
                b.RoomType == booking.RoomType &&
                b.CheckinDate.Date == booking.CheckinDate.Date &&
                b.CheckoutDate.Date == booking.CheckoutDate.Date
            );

            if (isDuplicate)
            {
                return BadRequest(new { message = "Duplicate booking detected." });
            }

            // Set default values
            booking.Status = string.IsNullOrWhiteSpace(booking.Status) ? "Pending" : booking.Status;
            booking.PaymentMethod = string.IsNullOrWhiteSpace(booking.PaymentMethod)
                ? "Cash Payment"
                : booking.PaymentMethod;

            if (string.IsNullOrWhiteSpace(booking.PaymentStatus))
            {
                booking.PaymentStatus = booking.PaymentMethod.Equals("Cash Payment", StringComparison.OrdinalIgnoreCase)
                    ? "Pending (Pay On Arrive)"
                    : "Complete";
            }

            booking.CreatedAt = DateTime.Now;

            room.Quantity -= 1;
            _context.Entry(room).State = EntityState.Modified;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBooking(int id, Booking updatedBooking)
        {
            if (id != updatedBooking.Id)
                return BadRequest("ID tidak sesuai.");

            var existingBooking = await _context.Bookings.FindAsync(id);
            if (existingBooking == null)
                return NotFound("Booking tidak ditemukan.");

            if (existingBooking.UserId != updatedBooking.UserId)
            {
                var user = await _context.Users.FindAsync(updatedBooking.UserId);
                if (user == null)
                    return BadRequest("User tidak ditemukan.");

                existingBooking.UserId = updatedBooking.UserId;
                existingBooking.Fullname = user.Fullname ?? "Guest";
                existingBooking.Email = user.Email ?? "unknown@example.com";
            }

            // Update all relevant fields
            existingBooking.CheckinDate = updatedBooking.CheckinDate;
            existingBooking.CheckoutDate = updatedBooking.CheckoutDate;
            existingBooking.RoomType = updatedBooking.RoomType;
            existingBooking.AdultGuests = updatedBooking.AdultGuests;
            existingBooking.ChildGuests = updatedBooking.ChildGuests;
            existingBooking.SpecialRequest = updatedBooking.SpecialRequest;
            existingBooking.TotalPrice = updatedBooking.TotalPrice;
            existingBooking.Status = updatedBooking.Status;
            existingBooking.PhoneNumber = updatedBooking.PhoneNumber;
            existingBooking.PaymentMethod = updatedBooking.PaymentMethod;
            existingBooking.PaymentStatus = updatedBooking.PaymentStatus;

            await _context.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchBooking(int id, [FromBody] JsonElement updates)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound("Booking tidak ditemukan.");

            foreach (var prop in updates.EnumerateObject())
            {
                var propertyName = prop.Name;

                // Skip properti yang tidak boleh diubah
                if (propertyName.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                    propertyName.Equals("userId", StringComparison.OrdinalIgnoreCase) ||
                    propertyName.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var propertyInfo = typeof(Booking).GetProperty(propertyName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    try
                    {
                        object value = null;

                        // Deteksi tipe data untuk konversi nilai
                        if (propertyInfo.PropertyType == typeof(string))
                            value = prop.Value.GetString();
                        else if (propertyInfo.PropertyType == typeof(int))
                            value = prop.Value.GetInt32();
                        else if (propertyInfo.PropertyType == typeof(DateTime))
                            value = prop.Value.GetDateTime();
                        else if (propertyInfo.PropertyType == typeof(decimal))
                            value = prop.Value.GetDecimal();
                        else if (propertyInfo.PropertyType == typeof(double))
                            value = prop.Value.GetDouble();
                        else if (propertyInfo.PropertyType == typeof(bool))
                            value = prop.Value.GetBoolean();
                        else
                            continue; // skip jika tipe tidak dikenali

                        propertyInfo.SetValue(booking, value);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"Gagal memproses properti '{propertyName}': {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
