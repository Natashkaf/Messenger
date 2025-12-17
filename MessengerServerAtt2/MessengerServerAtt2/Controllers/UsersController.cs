using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MessengerServerAtt2.Models;
using System.Security.Claims;

namespace MessengerServerAtt2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserId();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Username,
                    u.AvatarUrl,
                    u.Bio,
                    u.Status,
                    u.LastSeen,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return BadRequest(new { message = "Минимум 2 символа для поиска" });

            var userId = GetUserId();

            var users = await _context.Users
                .Where(u => u.Id != userId &&
                           (u.Username.Contains(query) || u.Email.Contains(query)))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.AvatarUrl,
                    u.Bio,
                    u.Status,
                    u.LastSeen
                })
                .Take(20)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            if (request.Username != user.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                    return BadRequest(new { message = "Имя пользователя уже занято" });
            }

            user.Username = request.Username;
            user.Bio = request.Bio;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Профиль обновлен" });
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userId);
        }
    }

    public class UpdateProfileRequest
    {
        public string Username { get; set; }
        public string Bio { get; set; }
    }
}