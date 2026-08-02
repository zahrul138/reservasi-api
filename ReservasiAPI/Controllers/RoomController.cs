using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasiAPI.Repository;
using ReservasiAPI.Repository.Models;

namespace ReservasiAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : ControllerBase
    {
        private readonly ReservasiDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RoomController(ReservasiDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            return await _context.Rooms.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return room;
        }

        [HttpPost]
        public async Task<ActionResult<Room>> CreateRoom([FromForm] Room room)
        {
            var form = Request.Form;

            var featuresRaw = form["features"].ToString();
            var amenitiesRaw = form["amenities"].ToString();
            var policiesRaw = form["policies"].ToString();

            room.Features = IsValidJson(featuresRaw) ? featuresRaw : "[]";
            room.Amenities = IsValidJson(amenitiesRaw) ? amenitiesRaw : "[]";
            room.Policies = IsValidJson(policiesRaw) ? policiesRaw : "[]";

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromForm] Room room)
        {
            if (id != room.Id) return BadRequest();

            var existingRoom = await _context.Rooms.FindAsync(id);
            if (existingRoom == null) return NotFound();

            var form = Request.Form;

            var featuresRaw = form["features"].ToString();
            var amenitiesRaw = form["amenities"].ToString();
            var policiesRaw = form["policies"].ToString();

            existingRoom.Features = IsValidJson(featuresRaw) ? featuresRaw : existingRoom.Features;
            existingRoom.Amenities = IsValidJson(amenitiesRaw) ? amenitiesRaw : existingRoom.Amenities;
            existingRoom.Policies = IsValidJson(policiesRaw) ? policiesRaw : existingRoom.Policies;

            var oldImage1 = existingRoom.Image1;
            var oldImage2 = existingRoom.Image2;
            var oldImage3 = existingRoom.Image3;

            existingRoom.Title = room.Title ?? existingRoom.Title;
            existingRoom.ShortDescription = room.ShortDescription ?? existingRoom.ShortDescription;
            existingRoom.FullDescription = room.FullDescription ?? existingRoom.FullDescription;
            existingRoom.Price = room.Price != 0 ? room.Price : existingRoom.Price;
            existingRoom.Size = room.Size ?? existingRoom.Size;
            existingRoom.Occupancy = room.Occupancy ?? existingRoom.Occupancy;
            existingRoom.Bed = room.Bed ?? existingRoom.Bed;
            existingRoom.RoomView = room.RoomView ?? existingRoom.RoomView;
            existingRoom.Image1 = room.Image1 ?? "";
            existingRoom.Image2 = room.Image2 ?? "";
            existingRoom.Image3 = room.Image3 ?? "";
            existingRoom.Quantity = room.Quantity;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rooms.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            if (oldImage1 != existingRoom.Image1) DeletePhysicalImage(oldImage1);
            if (oldImage2 != existingRoom.Image2) DeletePhysicalImage(oldImage2);
            if (oldImage3 != existingRoom.Image3) DeletePhysicalImage(oldImage3);

            return NoContent();
        }

        private void DeletePhysicalImage(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            if (!relativeUrl.StartsWith("/uploads/")) return;

            var fileName = Path.GetFileName(relativeUrl);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var uploadsDir = Path.Combine(_env.WebRootPath ?? "", "uploads");
            var filePath = Path.Combine(uploadsDir, fileName);

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception)
            {
            }
        }

        private static bool IsValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            if ((!input.StartsWith("[") || !input.EndsWith("]")) && (!input.StartsWith("{") || !input.EndsWith("}")))
                return false;
            try
            {
                var obj = System.Text.Json.JsonDocument.Parse(input);
                return true;
            }
            catch
            {
                return false;
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            DeletePhysicalImage(room.Image1);
            DeletePhysicalImage(room.Image2);
            DeletePhysicalImage(room.Image3);

            return NoContent();
        }

        [HttpPatch("{id}/quantity")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] UpdateQuantityDto dto)
        {
            if (dto.Quantity < 0)
            {
                return BadRequest("Quantity cannot be negative.");
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { id = room.Id, quantity = room.Quantity });
        }

        public class UpdateQuantityDto
        {
            public int Quantity { get; set; }
        }

        [HttpPatch("restore-quantity")]
        public async Task<IActionResult> RestoreRoomQuantity([FromBody] RestoreQuantityDto dto)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Title == dto.RoomType);
            if (room == null)
            {
                return NotFound("Room type not found.");
            }

            room.Quantity += 1;
            _context.Entry(room).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class RestoreQuantityDto
        {
            public string RoomType { get; set; }
        }
    }
}