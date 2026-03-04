using ManagementApp.Data;
using ManagementApp.Dtos;
using ManagementApp.Middleware;
using ManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? department = null)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var normalizedDepartment = department.Trim();
            query = query.Where(u => u.Department == normalizedDepartment);
        }

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "ID must be a positive integer" });
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    [RequireAuthentication]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        // Check if email already exists
        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail);

        if (existingUser)
        {
            return Conflict(new { message = "A user with this email already exists" });
        }

        var user = new User
        {
            FirstName = NormalizeRequired(request.FirstName),
            LastName = NormalizeRequired(request.LastName),
            Email = normalizedEmail,
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            Department = NormalizeOptional(request.Department),
            Position = NormalizeOptional(request.Position),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    // PUT: api/users/5
    [RequireAuthentication]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "ID must be a positive integer" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        // Check if email is being changed to an existing email
        if (existingUser.Email != normalizedEmail)
        {
            var emailExists = await _context.Users
            .AnyAsync(u => u.Id != id && u.Email == normalizedEmail);

            if (emailExists)
            {
                return Conflict(new { message = "A user with this email already exists" });
            }
        }

        // Update properties
        existingUser.FirstName = NormalizeRequired(request.FirstName);
        existingUser.LastName = NormalizeRequired(request.LastName);
        existingUser.Email = normalizedEmail;
        existingUser.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        existingUser.Department = NormalizeOptional(request.Department);
        existingUser.Position = NormalizeOptional(request.Position);
        existingUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(existingUser);
    }

    // DELETE: api/users/5
    [RequireAuthentication]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "ID must be a positive integer" });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [RequireAuthentication]
    // PATCH: api/users/5/deactivate
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "ID must be a positive integer" });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "User deactivated successfully", user });
    }

    [RequireAuthentication]
    // PATCH: api/users/5/activate
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "ID must be a positive integer" });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "User activated successfully", user });
    }

    // GET: api/users/debug/throw
    [HttpGet("debug/throw")]
    public IActionResult ThrowUnhandledForTesting([FromServices] IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException("Intentional test exception");
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
