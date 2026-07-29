using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ReservasiAPI.Models;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _serverKey = "SB-Mid-server-6r7IWmbYzEqf-s5Lg7Xtk9x2";

    public PaymentController()
    {
        _httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{_serverKey}:");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    [HttpPost("create-transaction")]
    public async Task<IActionResult> CreateTransaction([FromBody] MidtransRequest request)
    {
        var transaction = new
        {
            transaction_details = new
            {
                order_id = request.OrderId,
                gross_amount = request.Amount
            },
            customer_details = new
            {
                first_name = request.CustomerName,
                email = request.CustomerEmail
            },
            callbacks = new
            {
                finish = "http://localhost:3000/booking"
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(transaction), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://app.sandbox.midtrans.com/snap/v1/transactions", jsonContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(responseContent);

        var parsed = JsonDocument.Parse(responseContent);
        var token = parsed.RootElement.GetProperty("token").GetString();

        return Ok(new { token });
    }
    
}
