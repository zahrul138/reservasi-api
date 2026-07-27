using Microsoft.AspNetCore.Mvc;
using ReservasiAPI.Repository;
using ReservasiAPI.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace ReservasiAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ReservasiDbContext _context;
    private readonly IWebHostEnvironment _env;

    public UserController(ReservasiDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env; 
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("byemail/{email}")]
    public async Task<ActionResult<User>> GetUserByEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id)
            return BadRequest();

        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Fullname = request.Fullname;

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.Password = request.Password;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully",
            user = new
            {
                id = user.Id,
                fullname = user.Fullname,
                email = user.Email,
                role = user.Role
            }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(User user)
    {
        user.Role = string.IsNullOrEmpty(user.Role) ? "Guest" : user.Role;
        user.Createtime = DateTime.Now;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Login loginData)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginData.Email && u.Password == loginData.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Wrong email or password!" });
        }

        return Ok(new
        {
            message = "Sign in successful!",
            user = new
            {
                id = user.Id,
                fullname = user.Fullname,
                email = user.Email,
                role = user.Role
            }
        });
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] User adminUser)
    {
        if (!_env.IsDevelopment())
        {
            return BadRequest("Only available in development");
        }

        if (await _context.Users.AnyAsync(u => u.Email == adminUser.Email))
        {
            return BadRequest("Email already exists");
        }

        adminUser.Role = "Admin";
        adminUser.Createtime = DateTime.Now;

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Admin created successfully",
            userId = adminUser.Id
        });
    }
}

