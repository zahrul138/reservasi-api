using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasiAPI.Models;
using ReservasiAPI.Repository;
using ReservasiAPI.Repository.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReservasiAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ReservasiDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _serverKey;
        private readonly bool _isProduction;

        public PaymentController(
            ReservasiDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _serverKey = configuration["Midtrans:ServerKey"] ?? "";
            _isProduction = configuration.GetValue<bool>("Midtrans:IsProduction");
        }

        private HttpClient CreateMidtransClient()
        {
            var client = _httpClientFactory.CreateClient();
            var byteArray = Encoding.ASCII.GetBytes($"{_serverKey}:");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return client;
        }

        [HttpPost("create-transaction")]
        public async Task<IActionResult> CreateTransaction([FromBody] MidtransRequest request)
        {
            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null)
            {
                return NotFound(new { message = "Booking not found." });
            }

            if (!booking.PaymentStatus.Equals("Pending (Awaiting Payment)", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "This booking is not awaiting online payment." });
            }

            var orderId = $"BOOKING-{booking.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var transaction = new
            {
                transaction_details = new
                {
                    order_id = orderId,
                    gross_amount = (long)booking.TotalPrice
                },
                customer_details = new
                {
                    first_name = booking.Fullname,
                    email = booking.Email
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(transaction), Encoding.UTF8, "application/json");

            var baseUrl = _isProduction
                ? "https://app.midtrans.com/snap/v1/transactions"
                : "https://app.sandbox.midtrans.com/snap/v1/transactions";

            var client = CreateMidtransClient();
            var response = await client.PostAsync(baseUrl, jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(responseContent);
            }

            var parsed = JsonDocument.Parse(responseContent);
            var token = parsed.RootElement.GetProperty("token").GetString();

            booking.MidtransOrderId = orderId;
            await _context.SaveChangesAsync();

            return Ok(new { token });
        }

        [HttpPost("notification")]
        public async Task<IActionResult> HandleNotification([FromBody] MidtransNotification notification)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.OrderId))
            {
                return BadRequest();
            }

            var expectedSignature = ComputeSignature(
                notification.OrderId,
                notification.StatusCode,
                notification.GrossAmount);

            if (!string.Equals(expectedSignature, notification.SignatureKey, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "Invalid signature." });
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.MidtransOrderId == notification.OrderId);

            if (booking == null)
            {
                return NotFound();
            }

            await ApplyTransactionStatus(booking, notification.TransactionStatus, notification.FraudStatus);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("status/{bookingId}")]
        public async Task<IActionResult> CheckStatus(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new { message = "Booking not found." });
            }

            if (string.IsNullOrWhiteSpace(booking.MidtransOrderId))
            {
                return Ok(booking);
            }

            if (!booking.PaymentStatus.Equals("Pending (Awaiting Payment)", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(booking);
            }

            var baseUrl = _isProduction
                ? "https://api.midtrans.com/v2"
                : "https://api.sandbox.midtrans.com/v2";

            var client = CreateMidtransClient();
            var response = await client.GetAsync($"{baseUrl}/{booking.MidtransOrderId}/status");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Ok(booking);
            }

            var parsed = JsonDocument.Parse(responseContent);
            var root = parsed.RootElement;

            var transactionStatus = root.TryGetProperty("transaction_status", out var tsEl)
                ? tsEl.GetString()
                : null;
            var fraudStatus = root.TryGetProperty("fraud_status", out var fsEl)
                ? fsEl.GetString()
                : null;

            await ApplyTransactionStatus(booking, transactionStatus, fraudStatus);
            await _context.SaveChangesAsync();

            return Ok(booking);
        }

        private async Task ApplyTransactionStatus(Booking booking, string? transactionStatus, string? fraudStatus)
        {
            var status = transactionStatus?.ToLower();
            var fraud = fraudStatus?.ToLower();

            if (status == "capture")
            {
                booking.PaymentStatus = fraud == "accept" ? "Completed" : "Pending (Awaiting Payment)";
            }
            else if (status == "settlement")
            {
                booking.PaymentStatus = "Completed";
            }
            else if (status == "pending")
            {
                booking.PaymentStatus = "Pending (Awaiting Payment)";
            }
            else if (status == "deny" || status == "cancel" || status == "expire")
            {
                booking.PaymentStatus = "Failed";
                booking.Status = "Cancelled";

                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Title == booking.RoomType);
                if (room != null)
                {
                    room.Quantity += 1;
                    _context.Entry(room).State = EntityState.Modified;
                }
            }
        }

        private string ComputeSignature(string orderId, string statusCode, string grossAmount)
        {
            var raw = $"{orderId}{statusCode}{grossAmount}{_serverKey}";
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = SHA512.HashData(bytes);

            var builder = new StringBuilder();
            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}